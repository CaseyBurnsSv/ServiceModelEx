// © 2011 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;

[Serializable]
class Tuple<T1,T2>
{
   public Tuple(T1 item1,T2 item2)
   {
      Item1 = item1;
      Item2 = item2;
   }
   public readonly T1 Item1;
   public readonly T2 Item2;
 }

