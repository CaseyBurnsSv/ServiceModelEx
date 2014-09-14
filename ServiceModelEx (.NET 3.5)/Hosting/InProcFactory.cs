// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace ServiceModelEx
{
   public static class InProcFactory
   {
      static readonly string BaseAddress = "net.pipe://localhost/" + Guid.NewGuid();

      static readonly Binding Binding;

      static Dictionary<Type,Tuple<ServiceHost,EndpointAddress>> m_Hosts = new Dictionary<Type,Tuple<ServiceHost,EndpointAddress>>();
      static Dictionary<Type,ServiceThrottlingBehavior> m_Throttles = new Dictionary<Type,ServiceThrottlingBehavior>();
      static Dictionary<Type,object> m_Singletons = new Dictionary<Type,object>();

      static InProcFactory()
      {
         NetNamedPipeBinding binding;
         try
         {
            binding = new NetNamedPipeContextBinding("InProcFactory");
         }
         catch
         {
            binding = new NetNamedPipeContextBinding();
         }

         binding.TransactionFlow = true;
         Binding = binding;
         AppDomain.CurrentDomain.ProcessExit += delegate
                                                {
                                                   foreach(Tuple<ServiceHost,EndpointAddress> record in m_Hosts.Values)
                                                   {
                                                      record.Item1.Close();
                                                   }
                                                };
      }

      /// <summary>
      /// Can only call SetThrottle() before creating any instance of the service
      /// </summary>
      /// <typeparam name="S">Service type</typeparam>
      /// <param name="throttle">Throttle to use</param>
      [MethodImpl(MethodImplOptions.Synchronized)]
      public static void SetThrottle<S>(ServiceThrottlingBehavior throttle)
      {
         m_Throttles[typeof(S)] = throttle;
      }
      /// <summary>
      /// Can only call MaxThrottle() before creating any instance of the service
      /// </summary>
      public static void MaxThrottle<S>()
      {
         SetThrottle<S>(Int32.MaxValue,Int32.MaxValue,Int32.MaxValue);
      }
      /// <summary>
      /// Can only call SetThrottle() before creating any instance of the service
      /// </summary>
      public static void SetThrottle<S>(int maxCalls,int maxSessions,int maxInstances)
      {
         ServiceThrottlingBehavior throttle = new ServiceThrottlingBehavior();
         throttle.MaxConcurrentCalls = maxCalls;
         throttle.MaxConcurrentSessions = maxSessions;
         throttle.MaxConcurrentInstances = maxInstances;
         SetThrottle<S>(throttle);
      }
      /// <summary>
      /// Can only call SetSingleton() before creating any instance of the service
      /// </summary>
      /// <typeparam name="S"></typeparam>
      /// <param name="singleton"></param>
      [MethodImpl(MethodImplOptions.Synchronized)]
      public static void SetSingleton<S>(S singleton)
      {
         m_Singletons.Add(typeof(S),singleton);
      }

      [MethodImpl(MethodImplOptions.Synchronized)]
      public static I CreateInstance<S,I>() where I : class
                                            where S : class,I
      {
         EndpointAddress address = GetAddress<S,I>();
         ChannelFactory<I> factory = new ChannelFactory<I>(Binding,address);

         return factory.CreateChannel();
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      public static I CreateInstance<S,I,C>(InstanceContext<C> context) where I : class
                                                                        where S : class,I
      {
         EndpointAddress address = GetAddress<S,I>();
         DuplexChannelFactory<I,C> factory = new DuplexChannelFactory<I,C>(context,Binding,address);
         return factory.CreateChannel();
      }
      [MethodImpl(MethodImplOptions.Synchronized)]
      public static I CreateInstance<S,I,C>(C callback) where I : class
                                                             where S : class,I
      {
         DuplexClientBase<I,C>.VerifyCallback();
         InstanceContext<C> context = new InstanceContext<C>(callback);
         return CreateInstance<S,I,C>(context);
      }
      static EndpointAddress GetAddress<S,I>() where I : class
                                               where S : class,I
      {
         Tuple<ServiceHost,EndpointAddress> record;

         if(m_Hosts.ContainsKey(typeof(S)))
         {
            record = m_Hosts[typeof(S)];
         }
         else
         {
            ServiceHost<S> host;
            if(m_Singletons.ContainsKey(typeof(S)))
            {
               S singleton = m_Singletons[typeof(S)] as S;
               Debug.Assert(singleton != null);
               host = new ServiceHost<S>(singleton);
            }
            else
            {
               host = new ServiceHost<S>();
            }    
          
            string address =  BaseAddress + Guid.NewGuid();

            record = new Tuple<ServiceHost,EndpointAddress>(host,new EndpointAddress(address));
            m_Hosts[typeof(S)] = record;
            host.AddServiceEndpoint(typeof(I),Binding,address);

            if(m_Throttles.ContainsKey(typeof(S)))
            {
               host.SetThrottle(m_Throttles[typeof(S)]);
            }
            host.Open();
         }
         return record.Item2;
      }
      public static void CloseProxy<I>(I instance) where I : class
      {
         ICommunicationObject proxy = instance as ICommunicationObject;
         Debug.Assert(proxy != null);
         proxy.Close();
      }
   }
}