// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using Microsoft.ServiceBus;

namespace ServiceModelEx.ServiceBus
{
   public enum MessageDistributionPolicy
   {
      AllSubscribers,
      OneSubscriber
   }

   public class RouterPolicy : MessageBufferPolicy
   {
      public RouterPolicy()
      {
         throw new InvalidOperationException("Router emulation");
      }

      public int MaxSubscribers     
      {get;set;}
      public MessageDistributionPolicy MessageDistribution     
      {get;set;}
          
      public int PushDeliveryRetries     
      {get;set;}

      public DateTime ExpirationInstant
      {get;set;}  
    
      public int MaxBufferLength
      {get;set;} 
   }
   
   public static class RouterManagementClient
   {
      public static RouterClient CreateRouter(TransportClientEndpointBehavior credential,Uri routerUri,RouterPolicy policy)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public static void DeleteRouter(TransportClientEndpointBehavior credential,Uri routerUri)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public static RouterClient GetRouter(TransportClientEndpointBehavior credential,Uri routerUri)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public static RouterPolicy GetRouterPolicy(TransportClientEndpointBehavior credential,Uri routerUri)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public static DateTime RenewRouter(TransportClientEndpointBehavior credential,Uri routerUri,TimeSpan requestedExpiration)
      {
         throw new InvalidOperationException("Router emulation");
      }
   }
   public sealed class RouterClient
   {
      public RouterSubscriptionClient SubscribeToRouter(RouterClient routerClient,TimeSpan requestedTimeout)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public void DeleteRouter()
      {
         throw new InvalidOperationException("Router emulation");
      }
   }
   public class JunctionPolicy
   {}

   [Serializable]
   public class RouterSubscriptionClient
   {
      public DateTime Expires
      {get;set;}
      public DateTime Renew(TimeSpan requestedExpiration,TransportClientEndpointBehavior credential)
      {
         throw new InvalidOperationException("Router emulation");
      }
      public void Unsubscribe(TransportClientEndpointBehavior credential)
      {
         throw new InvalidOperationException("Router emulation");
      }
   }
}