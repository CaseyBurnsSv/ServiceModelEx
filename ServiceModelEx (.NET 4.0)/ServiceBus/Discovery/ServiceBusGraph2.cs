// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.ServiceBus;

namespace ServiceModelEx.ServiceBus
{
   public partial class ServiceBusGraph2
   {
      string Token
      {get;set;}

      string Namespace
      {get;set;}      
      
      string Secret
      {get;set;}

      string Issuer
      {get;set;}

      public ServiceBusNode[] DiscoveredEndpoints
      {get;private set;}

      string m_ServiceBusRootAddress;

      public string ServiceBusRootAddress
      {
         get
         {
            return m_ServiceBusRootAddress;
         }
         set
         {
            m_ServiceBusRootAddress = value;
            if(m_ServiceBusRootAddress.StartsWith(@"/"))
            {
               m_ServiceBusRootAddress = m_ServiceBusRootAddress.Remove(0,1);
            }
            if(m_ServiceBusRootAddress.EndsWith(@"/"))
            {
               m_ServiceBusRootAddress = m_ServiceBusRootAddress.Remove(m_ServiceBusRootAddress.Length-1,1);
            }
         }
      }
      public readonly TransportClientEndpointBehavior Credential;

      public ServiceBusGraph2(string serviceNamespace,string issuer,string secret)
      {
         Namespace = serviceNamespace;
         Secret = secret;
         Issuer = issuer;

         ServiceBusRootAddress = ServiceBusEnvironment.CreateServiceUri("https",serviceNamespace,"").AbsoluteUri;

         ServiceBusRootAddress = VerifyEndSlash(ServiceBusRootAddress);

         Credential = new TransportClientEndpointBehavior();
         Credential.CredentialType = TransportClientCredentialType.SharedSecret;

         Credential.Credentials.SharedSecret.IssuerName = Issuer;
         Credential.Credentials.SharedSecret.IssuerSecret = secret;
      }


      public ServiceBusNode[] Discover()
      {
         DiscoveredEndpoints = null;

         if(Token == null)
         {
            Token = GetToken(Namespace,Secret);
         }

         List<ServiceBusNode> nodes = Discover(ServiceBusRootAddress,null);

         Consolidate(nodes);

         DiscoveredEndpoints = SortList(nodes);

         AssertIntegrery(DiscoveredEndpoints);

         return DiscoveredEndpoints;
      }
      ServiceBusNode[] SortList(List<ServiceBusNode> nodes)
      {
         ServiceBusNode[] array = new ServiceBusNode[nodes.Count];

         for(int i = 0;i<array.Length;i++)
         {
            ServiceBusNode maxNode = FindMax(nodes);
            array[i] = maxNode;
            nodes.Remove(maxNode);
         }
         //Transpose array
         ServiceBusNode[] returned = new ServiceBusNode[array.Length];

         int index = 0;
         for(int j = array.Length-1;j>=0;j--)
         {
            returned[index++] = array[j];
         }
         return returned;
      }
      ServiceBusNode FindMax(List<ServiceBusNode> nodes)
      {
         ServiceBusNode maxNode = new ServiceBusNode("");
         foreach(ServiceBusNode node in nodes)
         {
            if(StringComparer.Ordinal.Compare(node.Name,maxNode.Name) >= 0)
            {
               maxNode = node;
            }
         }
         return maxNode;
      }

      List<ServiceBusNode> Discover(string root,ServiceBusNode router)
      {
         root = VerifyNoEndSlash(root);

         Uri feedUri = new Uri(root);

         List<ServiceBusNode> nodes = new List<ServiceBusNode>();

         if(root.Contains("!") == false)
         {
            string relativeAddress = root.Replace(ServiceBusRootAddress,"");
            if(relativeAddress != "" && relativeAddress != "/")
            {
               ServiceBusNode node = new ServiceBusNode(root);
               node.AddSubscribedTo(router);
               nodes.Add(node);
            }
         }

         SyndicationFeed feed = GetFeed(feedUri,Token);

         if(feed != null)
         {
            foreach(SyndicationItem endpoint in feed.Items)
            {
               ServiceBusNode node = null;

               foreach(SyndicationLink link in endpoint.Links)
               {
                  Trace.WriteLine("Link: " + link.RelationshipType + " " + link.Uri.AbsoluteUri);
                  //Try to threat as buffer
                  try
                  {
                     MessageBufferPolicy policy = GetBufferPolicy(link.Uri.AbsoluteUri);
                     node = new ServiceBusNode(link.Uri.AbsoluteUri);
                     node.Policy = policy;
                     nodes.Add(node);
                     break;
                  }
                  catch(FaultException exception)
                  {
                     Debug.Assert(exception.Message.StartsWith("Policy could not be retrieved"));
                  }
                  nodes.AddRange(Discover(link.Uri.AbsoluteUri,router));
               }
            }
         }
         return nodes;
      }
                  /* TODO Restore on next release
                  if(IsJunction(endpoint))
                  {
                     //Look for policies
                     if(link.RelationshipType == "alternate")
                     {
                        node = new ServiceBusNode(link.Uri.AbsoluteUri);
                        node.AddSubscribedTo(router);

                        if(endpoint.ElementExtensions[0].OuterName == "RouterPolicy")
                        {
                           node.Policy = GetRouterPolicy(link.Uri.AbsoluteUri);
                        }
                        if(endpoint.ElementExtensions[0].OuterName == "QueuePolicy")
                        {
                           node.Policy = GetBufferPolicy(link.Uri.AbsoluteUri);
                        }
                        nodes.Add(node);
                     }
                  }
                  //Look for subscribers
                  if(node != null)
                  {
                     if(node.Policy is RouterPolicy)
                     {
                        if(link.RelationshipType == "subscriptions")
                        {
                           List<ServiceBusNode> subscribers = Discover(link.Uri.AbsoluteUri,node);

                           foreach(ServiceBusNode subscriber in subscribers)
                           {
                              subscriber.Name = subscriber.Name.Replace(node.Name + "/","");
                           }
                           nodes.AddRange(subscribers);

                           node.SubscribersCount = (uint)(subscribers.Count);
                           node.Subscribers = subscribers.ToArray();
                        }
                     }
                  }
                  if(link.RelationshipType == "alternate" && node == null)
                  {
                     if(node == null)
                     {
                        nodes.AddRange(Discover(link.Uri.AbsoluteUri,router));
                     }
                     else
                     {
                        nodes.Add(node);
                        node = null;
                     }
                  }
                  */               
          
         
        // return nodes;

      string GetToken(string serviceNamespace,string password)
      {
         string token = null;

         //string tokenUri = string.Format("https://{0}/issuetoken.aspx?u={1}&p={2}",ServiceBusEnvironment.DefaultIdentityHostName,solutionName,Uri.EscapeDataString(solutionPassword));
         string tokenUri = Microsoft.ServiceBus.ServiceBusEnvironment.CreateServiceUri("https",serviceNamespace,"").AbsoluteUri;

         HttpWebRequest tokenRequest = WebRequest.Create(tokenUri) as HttpWebRequest;

         tokenRequest.Method = "GET";

         using(HttpWebResponse tokenResponse = tokenRequest.GetResponse() as HttpWebResponse)
         {
            StreamReader tokenStreamReader = new StreamReader(tokenResponse.GetResponseStream());

            token = tokenStreamReader.ReadToEnd();
         }
         return token;
      }
      static SyndicationFeed GetFeed(Uri feedUri,string token)
      {
         if(feedUri.Scheme != "http" && feedUri.Scheme != "https")
         {
            return null;
         }
         HttpWebRequest getFeedRequest = WebRequest.Create(feedUri) as HttpWebRequest;
         getFeedRequest.Method = "GET";
         getFeedRequest.Headers.Add("X-MS-Identity-Token",token);

         Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter();

         try
         {
            using(HttpWebResponse getFeedResponse = getFeedRequest.GetResponse() as HttpWebResponse)
            {
               atomFormatter.ReadFrom(new XmlTextReader(getFeedResponse.GetResponseStream()));
            }
         }
         catch
         {
         }
         return atomFormatter.Feed;
      }
      string VerifyEndSlash(string text)
      {
         Debug.Assert(text != null);

         if(text != String.Empty)
         {
            if(text.EndsWith("/") == false)
            {
               return text += "/";
            }
         }
         return text;
      }

      static string VerifyNoEndSlash(string text)
      {
         Debug.Assert(text != null);

         if(text != String.Empty)
         {
            if(text.EndsWith("/"))
            {
               return text.Remove(text.Length-1,1);
            }
         }
         return text;
      }      
      MessageBufferPolicy GetBufferPolicy(string address)
      {
         if(address.StartsWith(@"sb://"))
         {
            return null;
         }

         Uri bufferAddress = new Uri(address);

         MessageBufferClient client = MessageBufferClient.GetMessageBuffer(Credential,bufferAddress);
         return client.GetPolicy();
      }
   }
}