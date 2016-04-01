using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomApplicationRequestRouting
{
   public class Counter
   {
      public enum CounterType
      {
         String,
         Boolean,
         Double,
         Integer
      }

      public String Name { get; private set; }

      private Object _Value;
      public Object Value
      {
         get
         {
            switch (Type)
            {
               case CounterType.String:
                  return (String)_Value;
               case CounterType.Boolean:
                  return (Boolean)_Value;
               case CounterType.Double:
                  return (Double)_Value;
               case CounterType.Integer:
                  return (Int64)_Value;
               default:
                  return _Value;
            }
         }
         set { _Value = value; }
      }

      public CounterType Type;
      public Counter(String name, Object value, CounterType type)
      {
         Name = name;
         Value = value;
         Type = type;
      }
   }
}
