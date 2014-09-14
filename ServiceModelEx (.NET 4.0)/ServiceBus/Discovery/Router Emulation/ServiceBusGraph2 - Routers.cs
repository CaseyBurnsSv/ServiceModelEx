// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Windows.Forms;

namespace ServiceModelEx.ServiceBus
{
   public partial class ServiceBusGraph2
   {    
      void Consolidate(List<ServiceBusNode> nodes)
      {
         //Routers and buffers subscriber, they will appear twice - once as routers or queues and once as policies
         //Keep just the policies
         List<ServiceBusNode> nodesToRemove = new List<ServiceBusNode>();
         uint policiesCount = 0;

         foreach(ServiceBusNode junction in nodes)
         {
            if(junction.Policy != null)
            {
               policiesCount++;
               foreach(ServiceBusNode service in nodes)
               {
                  if(service.Name == junction.Name && service.Policy == null)
                  {
                     nodesToRemove.Add(service);
                     junction.AddSubscribedTo(service.SubscribedTo);
                     foreach(ServiceBusNode node in nodes)
                     {
                        node.ReplaceSubscriber(service,junction);
                     }
                  }
               }
            }
         }
         if(policiesCount < nodesToRemove.Count)
         {
            MessageBox.Show("Namespace feed has some inconsistencies","Service Bus Explorer",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
         }

         foreach(ServiceBusNode nodeToRemove in nodesToRemove)
         {
            nodes.Remove(nodeToRemove);
         }

         nodesToRemove.Clear();

         //Have only top-level items
         foreach(ServiceBusNode node in nodes)
         {
            if(node.SubscribedTo != null)
            {
               nodesToRemove.Add(node);
            }
         }

         foreach(ServiceBusNode node in nodesToRemove)
         {
            nodes.Remove(node);
         }

         //Remove all URIs that are prefex of others (the path leading to the address is reported as a seperate link
         nodesToRemove.Clear();

         foreach(ServiceBusNode part in nodes)
         {
            foreach(ServiceBusNode node in nodes)
            {
               if(node != part && node.Name.StartsWith(part.Name,StringComparison.OrdinalIgnoreCase))
               {
                  if(nodesToRemove.Contains(part) == false)
                  {
                     nodesToRemove.Add(part);
                  }
               }
            }
         }

         foreach(ServiceBusNode node in nodesToRemove)
         {
            nodes.Remove(node);
         }
      }
      void AssertIntegrery(ServiceBusNode[] array)
      {
         foreach(ServiceBusNode node in array)
         {
            if(node.SubscribersCount > 0)
            {
               Debug.Assert(node.Subscribers != null);
               foreach(ServiceBusNode subscriber in node.Subscribers)
               {
                  subscriber.SubscribedTo.Contains(node);
               }
            }
         }
      }
      static bool IsJunction(SyndicationItem item)
      {
         if(item.ElementExtensions.Count == 1)
         {
            return item.ElementExtensions[0].OuterName.Contains("Policy");
         }
         return false;
      }
      RouterPolicy GetRouterPolicy(string address)
      {
         address = address.Replace(@"https://",@"sb://");
         address = address.Replace(@"http://",@"sb://");

         Uri routerAddress = new Uri(address);

         return RouterManagementClient.GetRouterPolicy(Credential,routerAddress);
      }
   }
}