// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using Microsoft.ServiceBus;


namespace ServiceModelEx.ServiceBus
{
   static partial class ServiceBusHelper
   {
      internal static void VerifyOneway(Type interfaceType)
      {
         Debug.Assert(interfaceType.IsInterface);

         MethodInfo[] methods = interfaceType.GetMethods();
         foreach(MethodInfo method in methods)
         {
            object[] attributes = method.GetCustomAttributes(typeof(OperationContractAttribute),true);
            Debug.Assert(attributes.Length == 1);

            OperationContractAttribute attribute = attributes[0] as OperationContractAttribute;
            if(attribute.IsOneWay == false)
            {
               throw new InvalidOperationException("All operations on contract " + interfaceType + " must be one-way, but operation " + method.Name + " is not configured for one-way");
            }
         }
      }
      public static void DeleteBuffer(string bufferAddress,string secret)
      {
         if(bufferAddress.EndsWith("/") == false)
         {
            bufferAddress += "/";
         }         
         
         Uri address = new Uri(bufferAddress);

         TransportClientEndpointBehavior credential = new TransportClientEndpointBehavior();
         credential.CredentialType = TransportClientCredentialType.SharedSecret;
         credential.Credentials.SharedSecret.IssuerName = DefaultIssuer;
         credential.Credentials.SharedSecret.IssuerSecret = secret;

         if(BufferExists(address,credential))
         {
            MessageBufferClient client = MessageBufferClient.GetMessageBuffer(credential,address);
            client.DeleteMessageBuffer();
         }  
      }
      public static void CreateBuffer(string bufferAddress,string secret)
      {
         CreateBuffer(bufferAddress,ServiceBusHelper.DefaultIssuer,secret);
      }
      public static void CreateBuffer(string bufferAddress,string issuer,string secret)
      {
         TransportClientEndpointBehavior credential = new TransportClientEndpointBehavior();
         credential.CredentialType = TransportClientCredentialType.SharedSecret;
         credential.Credentials.SharedSecret.IssuerName = issuer;
         credential.Credentials.SharedSecret.IssuerSecret = secret;

         CreateBuffer(bufferAddress,credential);
      }
      static void CreateBuffer(string bufferAddress,TransportClientEndpointBehavior credential)
      {
         MessageBufferPolicy policy = CreateBufferPolicy();
         CreateBuffer(bufferAddress,policy,credential);
      }
      static internal MessageBufferPolicy CreateBufferPolicy()
      {
         MessageBufferPolicy policy = new MessageBufferPolicy();                
         policy.Discoverability = DiscoverabilityPolicy.Public;
         policy.ExpiresAfter = TimeSpan.FromMinutes(10);
         policy.MaxMessageCount = 50;

         return policy;
      }
      public static void VerifyBuffer(string bufferAddress,string secret)
      {
         VerifyBuffer(bufferAddress,ServiceBusHelper.DefaultIssuer,secret);
      }
      public static void VerifyBuffer(string bufferAddress,string issuer,string secret)
      {
         TransportClientEndpointBehavior credential = new TransportClientEndpointBehavior();
         credential.CredentialType = TransportClientCredentialType.SharedSecret;
         credential.Credentials.SharedSecret.IssuerName = issuer;
         credential.Credentials.SharedSecret.IssuerSecret = secret;

         VerifyBuffer(bufferAddress,credential);
      }
      internal static void VerifyBuffer(string bufferAddress,TransportClientEndpointBehavior credential)
      {
         if(BufferExists(bufferAddress,credential))
         {
            return;
         }
         CreateBuffer(bufferAddress,credential);
      }
      public static void PurgeBuffer(Uri bufferAddress,TransportClientEndpointBehavior credential)
      {
         Debug.Assert(BufferExists(bufferAddress,credential));

         MessageBufferClient client = MessageBufferClient.GetMessageBuffer(credential,bufferAddress);
         MessageBufferPolicy policy = client.GetPolicy();
         client.DeleteMessageBuffer();
         MessageBufferClient.CreateMessageBuffer(credential,bufferAddress,policy);
      }
      //Helpers
      internal static bool BufferExists(string bufferAddress,TransportClientEndpointBehavior credential)
      {
         return BufferExists(new Uri(bufferAddress),credential);
      }
      internal static bool BufferExists(Uri bufferAddress,TransportClientEndpointBehavior credential)
      {
         try
         {
            MessageBufferClient client = MessageBufferClient.GetMessageBuffer(credential,bufferAddress);
            MessageBufferPolicy policy  = client.GetPolicy();
            if(policy.TransportProtection != TransportProtectionPolicy.AllPaths)
            {
               throw new InvalidOperationException("Buffer must be configured for transport protection");
            }
            return true;
         }
         catch(FaultException exception)
         {
            Debug.Assert(exception.Message == "Policy could not be retrieved: ContentType is incorrect");
         }
         
         return false;
      }
      static void CreateBuffer(string bufferAddress,MessageBufferPolicy policy,TransportClientEndpointBehavior credential)
      {
         if(bufferAddress.EndsWith("/") == false)
         {
            bufferAddress += "/";
         }         
         
         Uri address = new Uri(bufferAddress);

         if(BufferExists(address,credential))
         {
            MessageBufferClient client = MessageBufferClient.GetMessageBuffer(credential,address);
            client.DeleteMessageBuffer();
         }  
         MessageBufferClient.CreateMessageBuffer(credential,address,policy);
      }
   }
}






