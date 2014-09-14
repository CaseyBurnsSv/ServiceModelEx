// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Microsoft.ServiceBus;


namespace ServiceModelEx.ServiceBus
{
   public partial class DiscoverableServiceHost : ServiceHost,IServiceBusProperties
   {
      public DiscoverableServiceHost(object singletonInstance,params Uri[] baseAddresses) : base(singletonInstance,baseAddresses)
      {
         EnableDiscovery();
      }

      public DiscoverableServiceHost(Type serviceType,params Uri[] baseAddresses) : base(serviceType,baseAddresses)
      {
         EnableDiscovery();
      }
    

      void EnableDiscovery()
      {
         Debug.Assert(State != CommunicationState.Opened);

         IEndpointBehavior registryBehavior = new ServiceRegistrySettings(DiscoveryType.Public);
         foreach(ServiceEndpoint endpoint in Description.Endpoints)
         {
            endpoint.Behaviors.Add(registryBehavior);
         }
      }

      TransportClientEndpointBehavior IServiceBusProperties.Credential
      {
         [MethodImpl(MethodImplOptions.Synchronized)]
         get
         {
            TransportClientEndpointBehavior credentials = null;

            foreach(ServiceEndpoint endpoint in Description.Endpoints)
            {
               credentials = endpoint.Behaviors.Find<TransportClientEndpointBehavior>();
               if(credentials != null)
               {
                  break;
               }
            }
            Debug.Assert(credentials != null);

            return credentials;
         }
         [MethodImpl(MethodImplOptions.Synchronized)]
         set
         {
            Debug.Assert(State != CommunicationState.Opened);
            foreach(ServiceEndpoint endpoint in Description.Endpoints)
            {
               Debug.Assert(endpoint.Behaviors.Contains(typeof(TransportClientEndpointBehavior)) == false,"Do not add credentials mutiple times");
               endpoint.Behaviors.Add(value);
            }
         }
      }
      Uri[] IServiceBusProperties.Addresses
      {
         [MethodImpl(MethodImplOptions.Synchronized)]
         get
         {
            return Addresses;
         }
      }
      protected virtual Uri[] Addresses
      {
         [MethodImpl(MethodImplOptions.Synchronized)]
         get
         {
            List<Uri> addresses = new List<Uri>();

            foreach(ServiceEndpoint endpoint in Description.Endpoints)
            {
               addresses.Add(endpoint.Address.Uri);
            }
            return addresses.ToArray();
         }
      }
   }
}

