// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net


using System.Collections.Generic;
using System.Diagnostics;

namespace ServiceModelEx.ServiceBus
{
   public partial class ServiceBusNode
   {
      //TODO Restore on next release 
      // public JunctionPolicy Policy
      //{
      //   get;
      //   set;
      //}

      public ServiceBusNode[] Subscribers;

      public ServiceBusNode[] SubscribedTo
      {get;set;}

      public uint SubscribersCount
      {get;set;}

      public void AddSubscribedTo(ServiceBusNode[] subscribedTo)
      {
         if(SubscribedTo == null)
         {
            SubscribedTo = subscribedTo;
            return;
         }
         if(subscribedTo != null)
         {
            List<ServiceBusNode> list = new List<ServiceBusNode>(SubscribedTo);
            list.AddRange(subscribedTo);
            SubscribedTo = list.ToArray();
         }
      }

      public void AddSubscribedTo(ServiceBusNode subscribedTo)
      {
         if(subscribedTo == null)
         {
            return;
         }
         AddSubscribedTo(new ServiceBusNode[] { subscribedTo });
      }

      public void ReplaceSubscriber(ServiceBusNode service,ServiceBusNode junction)
      {
         if(Subscribers == null)
         {
            return;
         }
         Debug.Assert(service != null);
         Debug.Assert(junction != null);

         List<ServiceBusNode> list = new List<ServiceBusNode>(Subscribers);
         if(list.Contains(service))
         {
            list.Remove(service);
            list.Add(junction);
         }
         Subscribers = list.ToArray();
      }

      public void AddSubscriber(ServiceBusNode junction)
      {
         Debug.Assert(junction != null);

         if(Subscribers == null)
         {
            Subscribers = new ServiceBusNode[] { junction };
            return;
         }

         List<ServiceBusNode> list = new List<ServiceBusNode>(Subscribers);

         Debug.Assert(list.Contains(junction) == false);

         list.Add(junction);

         Subscribers = list.ToArray();
      }  
   }
}