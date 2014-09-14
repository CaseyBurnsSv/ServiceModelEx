// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net


using System;
using System.ServiceModel.Persistence;

namespace ServiceModelEx
{
   public class TransactionalInstanceProviderFactory : MemoryProviderFactory
   {
      public override PersistenceProvider CreateProvider(Guid id)
      {
         return new TransactionalInstanceProvider(id);
      }
   }
}