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
   public class BufferedServiceBusChannelFactory<T> : HeaderChannelFactory<T,ResponseContext> where T : class
   {
      static BufferedServiceBusChannelFactory()
      {
         ServiceBusHelper.VerifyOneway(typeof(T));
      }
      MessageBufferClient m_BufferClient;

      //Address from config

      public BufferedServiceBusChannelFactory() 
      {}
      public BufferedServiceBusChannelFactory(string secret) 
      {
         this.SetServiceBusCredentials(secret);
      }
      
      public BufferedServiceBusChannelFactory(string endpointName,string issuer,string secret) : base(endpointName) 
      {
         this.SetServiceBusCredentials(issuer,secret);
      }

      //No need for config file
      public BufferedServiceBusChannelFactory(Uri queueAddress) : base(new NetOnewayRelayBinding(),new EndpointAddress(queueAddress)) 
      {}
      public BufferedServiceBusChannelFactory(Uri queueAddress,string secret) : this(queueAddress)
      {
         this.SetServiceBusCredentials(secret);
      }

      public new T CreateChannel()
      {
         Debug.Assert(Endpoint.Binding is NetOnewayRelayBinding);
         ServiceBusHelper.VerifyBuffer(Endpoint.Address.Uri.AbsoluteUri,ServiceBusCredentials);

         m_BufferClient = MessageBufferClient.GetMessageBuffer(ServiceBusCredentials,Endpoint.Address.Uri);

         return base.CreateChannel();
      }

      protected override void PreInvoke(ref Message request)
      {
         base.PreInvoke(ref request);
                     
         m_BufferClient.Send(request,TimeSpan.MaxValue);
      }

      protected virtual string Enqueue(Action action) 
      {
         try
         {
            action();
         }
         catch(InvalidOperationException exception)
         {
            Debug.Assert(exception.Message == "This message cannot support the operation because it has been written.");
         }
         return null;
      }

      protected TransportClientEndpointBehavior ServiceBusCredentials
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
               Endpoint.Behaviors.Add(credential);
               return credential;
            }
         }
      }
   }
}