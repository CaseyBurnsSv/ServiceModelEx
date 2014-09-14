// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.ServiceBus;

namespace ServiceModelEx.ServiceBus
{
   public static partial class ServiceBusHelper
   {
      public const string DefaultIssuer = "owner";

      static void SetServiceBusCredentials(IEnumerable<ServiceEndpoint> endpoints,string issuer,string secret)
      {
         TransportClientEndpointBehavior behavior = new TransportClientEndpointBehavior();
         behavior.CredentialType = TransportClientCredentialType.SharedSecret;
         behavior.Credentials.SharedSecret.IssuerName = issuer;
         behavior.Credentials.SharedSecret.IssuerSecret = secret;

         SetBehavior(endpoints,behavior);
      }
      
      public static void SetServiceBusCredentials<T>(this ClientBase<T> proxy,string secret) where T : class
      {
         if(proxy.State == CommunicationState.Opened)
         {
            throw new InvalidOperationException("Proxy is already opened");
         }
         proxy.SetServiceBusCredentials(DefaultIssuer,secret);
      }
      public static void SetServiceBusCredentials<T>(this ClientBase<T> proxy,string issuer,string secret) where T : class
      {
         if(proxy.State == CommunicationState.Opened)
         {
            throw new InvalidOperationException("Proxy is already opened");
         }
         proxy.ChannelFactory.SetServiceBusCredentials(issuer,secret);
      }

      public static void SetServiceBusCredentials<T>(this ChannelFactory<T> factory,string issuer,string secret) where T : class
      {
         if(factory.State == CommunicationState.Opened)
         {
            throw new InvalidOperationException("Factory is already opened");
         }

         ServiceEndpoint[] endpoints = {factory.Endpoint};

         SetServiceBusCredentials(endpoints,issuer,secret);
      }
      public static void SetServiceBusCredentials<T>(this ChannelFactory<T> factory,string secret) where T : class
      {
         factory.SetServiceBusCredentials(DefaultIssuer,secret);
      }
      public static void SetServiceBusCredentials(this ServiceHost host,string secret)
      {
         if(host.State == CommunicationState.Opened)
         {
            throw new InvalidOperationException("Host is already opened");
         }
         SetServiceBusCredentials(host.Description.Endpoints,DefaultIssuer,secret);
      }
      public static void SetServiceBusCredentials(this ServiceHost host,string issuer,string secret)
      {
         if(host.State == CommunicationState.Opened)
         {
            throw new InvalidOperationException("Host is already opened");
         }
         SetServiceBusCredentials(host.Description.Endpoints,issuer,secret);
      }       
      public static void SetServiceBusCredentials(this MetadataExchangeClient mexClient,string secret)
      {
         SetServiceBusCredentials(mexClient,DefaultIssuer,secret);
      }
      public static void SetServiceBusCredentials(this MetadataExchangeClient mexClient,string issuer,string secret)
      {
         Type type = mexClient.GetType();
         FieldInfo info = type.GetField("factory",BindingFlags.Instance|BindingFlags.NonPublic);
         ChannelFactory<IMetadataExchange> factory = info.GetValue(mexClient) as ChannelFactory<IMetadataExchange>;
         factory.SetServiceBusCredentials(issuer,secret);
      }       
   }
}





