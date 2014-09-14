// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.ServiceBus;

namespace ServiceModelEx.ServiceBus
{
   public abstract class BufferedServiceBusClient<T> : HeaderClientBase<T,ResponseContext>,IServiceBusProperties where T : class
   {
      MessageBufferClient m_BufferClient;

      static BufferedServiceBusClient()
      {
         ServiceBusHelper.VerifyOneway(typeof(T));
      }

      //Address from config

      public BufferedServiceBusClient() 
      {}
      public BufferedServiceBusClient(string secret) 
      {
         this.SetServiceBusCredentials(secret);
      }
      public BufferedServiceBusClient(string issuer,string secret) 
      {
         this.SetServiceBusCredentials(issuer,secret);
      }  
      public BufferedServiceBusClient(string endpointName,string issuer,string secret) : base(endpointName) 
      {
         this.SetServiceBusCredentials(issuer,secret);
      }
      //No need for config file
      public BufferedServiceBusClient(Uri bufferAddress,string secret) : this(bufferAddress,ServiceBusHelper.DefaultIssuer,secret) 
      {}
      public BufferedServiceBusClient(Uri bufferAddress,string isssuer,string secret) : base(new NetOnewayRelayBinding(),new EndpointAddress(bufferAddress)) 
      {
         this.SetServiceBusCredentials(isssuer,secret);
      }
      public BufferedServiceBusClient(Uri bufferAddress) : base(new NetOnewayRelayBinding(),new EndpointAddress(bufferAddress)) 
      {}
      protected virtual void Enqueue(Action action) 
      {
         try
         {
            action();
         }
         catch(InvalidOperationException exception)
         {
            Debug.Assert(exception.Message == "This message cannot support the operation because it has been written.");
         }
      }
      protected override T CreateChannel()
      {
         Debug.Assert(Endpoint.Binding is NetOnewayRelayBinding);

         Uri bufferAddress = new Uri("https://" + Endpoint.Address.Uri.Host + Endpoint.Address.Uri.LocalPath);
         ServiceBusHelper.VerifyBuffer(bufferAddress.AbsoluteUri,Credential);

         m_BufferClient = MessageBufferClient.GetMessageBuffer(Credential,bufferAddress);

         return base.CreateChannel();
      }

      protected override void PreInvoke(ref Message request)
      {
         base.PreInvoke(ref request);
                     
         m_BufferClient.Send(request);
      }
      protected TransportClientEndpointBehavior Credential
      {
         get
         {
            IServiceBusProperties properties = this;
            return properties.Credential;
         }
         set
         {
            IServiceBusProperties properties = this;
            properties.Credential = value;
         }
      }

      TransportClientEndpointBehavior IServiceBusProperties.Credential
      {
         get
         {
            if(Endpoint.Behaviors.Contains(typeof(TransportClientEndpointBehavior)))
            {
               return Endpoint.Behaviors.Find<TransportClientEndpointBehavior>();
            }
            else
            {
               TransportClientEndpointBehavior credential = new TransportClientEndpointBehavior();
               Credential = credential;
               return Credential;
            }
         }
         set
         {
            Debug.Assert(Endpoint.Behaviors.Contains(typeof(TransportClientEndpointBehavior)) == false);
            Endpoint.Behaviors.Add(value);
         }
      }

      Uri[] IServiceBusProperties.Addresses
      {
         get
         {
            return new Uri[]{Endpoint.Address.Uri};
         }
      }
   }
}




 
