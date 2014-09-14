// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;
using Microsoft.ServiceBus;

namespace ServiceModelEx.ServiceBus
{
   public class BufferedServiceBusHost<T> : ServiceHost<T>,IServiceBusProperties 
   {
      Uri[] m_BufferAddresses;
      List<Thread> m_RetrievingThreads;      
      IChannelFactory<IDuplexSessionChannel> m_Factory;
      Dictionary<string,IDuplexSessionChannel> m_Proxies;

      TransportClientEndpointBehavior m_Credential;

      const string CloseAction = "ServiceModelEx.ServiceBus.BufferedServiceBusHost.CloseThread";
      
      public BufferedServiceBusHost(string secret,params Uri[] bufferAddresses) : this(ServiceBusHelper.DefaultIssuer,secret,bufferAddresses)
      {}
      public BufferedServiceBusHost(string issuer,string secret,params Uri[] bufferAddresses)
      {
         CommonConstruct(bufferAddresses);

         m_Credential.Credentials.SharedSecret.IssuerName = issuer;
         m_Credential.Credentials.SharedSecret.IssuerSecret = secret;
      }
      public BufferedServiceBusHost(T singleton,string secret,params Uri[] bufferAddresses) : this(singleton,ServiceBusHelper.DefaultIssuer,secret,bufferAddresses)
      {}
      public BufferedServiceBusHost(T singleton,string issuer,string secret,params Uri[] bufferAddresses) : base(singleton)
      {
         CommonConstruct(bufferAddresses);

         m_Credential.Credentials.SharedSecret.IssuerName = issuer;
         m_Credential.Credentials.SharedSecret.IssuerSecret = secret;
      }

      protected override void OnOpening()
      {
         ConfigureServiceBehavior();
         base.OnOpening();
      }
      protected override void OnOpened()
      {
         CreateProxies();                       
         CreateListeners();

         base.OnOpened();
      }
      protected override void OnClosing()
      {
         CloseListeners();

         foreach(IDuplexSessionChannel proxy in m_Proxies.Values)
         {
            try
            {
               proxy.Close();
            }
            catch
            {}
         }
         m_Factory.Close();

         PurgeBuffers();
         base.OnClosing();
      }
      public new void Abort()
      {
         AbortListeners();

         foreach(IDuplexSessionChannel proxy in m_Proxies.Values)
         {
            try
            {
               proxy.Abort();
            }
            catch
            {}
         }
         m_Factory.Abort();

         base.Abort();
      }
           
      void CommonConstruct(Uri[] bufferAddresses)
      {         
         Debug.Assert(bufferAddresses != null);
         Debug.Assert(bufferAddresses.Length >= 1,"You must specify at least one buffer's address");

         m_BufferAddresses = bufferAddresses;

         string serviceNamespace = ServiceBusHelper.ExtractNamespace(m_BufferAddresses[0]);

         //Sanity check - should all use same service namespace
         foreach(Uri baseAddress in m_BufferAddresses)
         {
            Debug.Assert(serviceNamespace == ServiceBusHelper.ExtractNamespace(baseAddress));
            Debug.Assert(baseAddress.Scheme == "https");
         }

         m_Credential = new TransportClientEndpointBehavior();
         m_Credential.CredentialType = TransportClientCredentialType.SharedSecret;

         InitializeHost();
      }
      void InitializeHost()
      {
         Debug.Assert(Description.Endpoints.Count == 0,"Please do not include endpoints in config. Instead, provide buffer addresses to the constructor");

         Binding binding = new NetNamedPipeBinding();
         binding.SendTimeout = TimeSpan.MaxValue;

         Type[] interfaces = typeof(T).GetInterfaces();
         Debug.Assert(interfaces.Length > 0);

         foreach(Type interfaceType in interfaces)
         {
            if(interfaceType.GetCustomAttributes(typeof(ServiceContractAttribute),false).Length == 1)
            {
               ServiceBusHelper.VerifyOneway(interfaceType);
               string address = @"net.pipe://localhost/" + Guid.NewGuid();
               AddServiceEndpoint(interfaceType,binding,address);
            }
         }
         m_Factory = binding.BuildChannelFactory<IDuplexSessionChannel>();
         m_Factory.Open();
      }

      void ConfigureServiceBehavior()
      {
         ServiceBehaviorAttribute behavior = Description.Behaviors.Find<ServiceBehaviorAttribute>();
         if(behavior.InstanceContextMode != InstanceContextMode.Single)
         {
            behavior.InstanceContextMode = InstanceContextMode.PerCall;
            behavior.ConcurrencyMode = ConcurrencyMode.Multiple;

            foreach(ServiceEndpoint endpoint in Description.Endpoints)
            {
               foreach(OperationDescription operation in endpoint.Contract.Operations)
               {
                  OperationBehaviorAttribute attribute = operation.Behaviors.Find<OperationBehaviorAttribute>();
                  if(attribute.TransactionScopeRequired == true)
                  {
                     behavior.ReleaseServiceInstanceOnTransactionComplete = false;
                     return;
                  }
               }
            }
         }
      }
      void CreateProxies()
      {
         m_Proxies = new Dictionary<string,IDuplexSessionChannel>();

         foreach(ServiceEndpoint endpoint in Description.Endpoints)
         {
            IDuplexSessionChannel channel = m_Factory.CreateChannel(endpoint.Address);
            channel.Open();

            m_Proxies[endpoint.Contract.Name] = channel;
         }
      }
      void CreateListeners()
      {
         m_RetrievingThreads = new List<Thread>();

         foreach(Uri bufferAddress in m_BufferAddresses)
         {
            ServiceBusHelper.VerifyBuffer(bufferAddress.AbsoluteUri,m_Credential);

            Thread thread = new Thread(Dequeue);
            m_RetrievingThreads.Add(thread);

            thread.IsBackground = true;
            thread.Start(bufferAddress);
         }
      }
      void Dequeue(object arg)
      {
         Uri bufferAddress = arg as Uri;
         Debug.Assert(bufferAddress != null);

         MessageBufferClient bufferClient = MessageBufferClient.GetMessageBuffer(m_Credential,bufferAddress);
         while(true)
         {
            Message message = null;

            try
            {
               message = bufferClient.Retrieve();
            }
            catch(TimeoutException)
            {
               Trace.WriteLine("Timed out before retirieving message"); 
               continue;
            }
            if(message.Headers.Action == CloseAction)
            {
               return;
            }
            else
            {
               Dispatch(message);
            }
         }
      }
      void Dispatch(Message message)
      {
         string contract = ExtractContract(message);
         if(contract == null)
         {
            return;
         }
         try
         {
            m_Proxies[contract].Send(message);
         }
         catch
         {
            m_Proxies[contract].Abort();
            IDuplexSessionChannel channel = m_Factory.CreateChannel(m_Proxies[contract].RemoteAddress);
            channel.Open();
            m_Proxies[contract] = channel;
         }
      }
      static string ExtractContract(Message message)
      {
         if(message.Headers.Action.Contains('/') == false)
         {
            return null;
         }
         string[] elements = message.Headers.Action.Split('/');
         return elements[elements.Length-2];         
      }

      void SendCloseMessages()
      {
         foreach(Uri bufferAddress in m_BufferAddresses)
         {
            MessageBufferClient bufferClient = MessageBufferClient.GetMessageBuffer(m_Credential,bufferAddress);
            Message message = Message.CreateMessage(MessageVersion.Default,CloseAction);
            bufferClient.Send(message);
         }
      }
      void CloseListeners()
      {
         SendCloseMessages();

         foreach(Thread thread in m_RetrievingThreads)
         {
            thread.Join();
         }
      }
      void AbortListeners()
      {
         foreach(Thread thread in m_RetrievingThreads)
         {
            thread.Abort();
            thread.Join();
         }
      }
      [Conditional("DEBUG")]
      void PurgeBuffers()
      {
         foreach(Uri bufferAddress in m_BufferAddresses)
         {
            try
            {
               ServiceBusHelper.PurgeBuffer(bufferAddress,m_Credential);
            }
            catch
            {}
         }
      }

      TransportClientEndpointBehavior IServiceBusProperties.Credential
      {
         get
         {
            return m_Credential;
         }
         set
         {
            m_Credential = value;
         }
      }

      Uri[] IServiceBusProperties.Addresses
      {
         get
         {
            return m_BufferAddresses;
         }
      }
   }
}