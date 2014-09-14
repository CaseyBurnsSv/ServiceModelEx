// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel.Channels;

namespace ServiceModelEx.ServiceBus
{
   public abstract class ClientBufferResponseBase<T> : BufferedServiceBusClient<T> where T : class
   {
      protected readonly Uri ResponseAddress;

      public ClientBufferResponseBase(string secret,Uri responseAddress) : base(secret)
      {
         ResponseAddress = responseAddress;
      }
      public ClientBufferResponseBase(string endpointName,string secret,Uri responseAddress) : base(endpointName,secret)
      {
         ResponseAddress = responseAddress;
      }
      public ClientBufferResponseBase(string endpointName,string issuer,string secret,Uri responseAddress) : base(endpointName,issuer,secret)
      {
         ResponseAddress = responseAddress;
      }
      public ClientBufferResponseBase(Uri serviceAddress,string secret,Uri responseAddress) : base(serviceAddress,secret)
      {
         ResponseAddress = responseAddress;
      }
      public ClientBufferResponseBase(Uri serviceAddress,string issuer,string secret,Uri responseAddress) : base(serviceAddress,issuer,secret)
      {
         ResponseAddress = responseAddress;
      }
      protected override void PreInvoke(ref Message request)
      {
         string methodId = GenerateMethodId();
         Header = new ResponseContext(ResponseAddress.AbsoluteUri,methodId);
         base.PreInvoke(ref request);
      }

      protected virtual string GenerateMethodId()
      {
         return Guid.NewGuid().ToString();
      }
   }
}

