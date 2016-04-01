using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace CustomApplicationRequestRouting
{
   public class WebFarm
   {
      public List<Server> Servers { get; private set; }
      public class Server
      {
         [Flags]
         public enum RunTimeState
         {
            Unknown = -1,
            Active = 0,
            Draining = 1,
            Unavailable = 2,
            UnvailableGracefully = 3
         }

         public String Name { get; private set; }
         public Int32 Weight { get; private set; }
         public Int32 HttpPort { get; private set; }
         public Int32 HttpsPort { get; private set; }
         public Boolean Healthy { get; private set; }
         /// <summary>
         /// Status == Online ? true : false
         /// </summary>
         public Boolean Enabled { get; private set; }

         public RunTimeState ServerState { get; private set; }

         public List<Counter> Counters { get; private set; }
         public Server(ConfigurationElement el)
         {
            Counters = new List<Counter>();
            Name = el.GetAttributeValue("address").ToString();

            var arrConfig = el.GetChildElement("applicationRequestRouting");
            var counters = arrConfig.GetChildElement("counters");
            Healthy = Boolean.Parse(counters.GetAttribute("isHealthy").Value.ToString());
            Enabled = Boolean.Parse(el.GetAttribute("enabled").Value.ToString());
            Weight = Int32.Parse(arrConfig.GetAttributeValue("weight").ToString());
            HttpPort = Int32.Parse(arrConfig.GetAttributeValue("httpPort").ToString());
            HttpsPort = Int32.Parse(arrConfig.GetAttributeValue("httpPort").ToString());

            RunTimeState tempState = RunTimeState.Unknown;
            if (Enum.TryParse(counters.GetAttribute("state").Value.ToString(), out tempState))
            {
               // alert if not parseable?
               ServerState = tempState;
            }

            foreach (var c in counters.Attributes)
            {
               Int64 intValue = 0;
               Boolean boolValue = false;
               Double dblValue = 0.0;
               if (Int64.TryParse(c.Value.ToString(), out intValue))
                  Counters.Add(new Counter(c.Name, intValue, Counter.CounterType.Integer));
               else if (Double.TryParse(c.Value.ToString(), out dblValue))
                  Counters.Add(new Counter(c.Name, dblValue, Counter.CounterType.Double));
               else if (Boolean.TryParse(c.Value.ToString(), out boolValue))
                  Counters.Add(new Counter(c.Name, boolValue, Counter.CounterType.Boolean));
               else
                  Counters.Add(new Counter(c.Name, c.Value.ToString(), Counter.CounterType.String));
            }

         }
      }



      [Flags]
      public enum FarmState
      {
         Failed = 0,
         Degraded = 1,
         Normal = 2,
         Bypass = 3,
      }

      public String Name { get; private set; }
      public Boolean Enabled { get; private set; }
      public Int32 MinimumServers { get; private set; }
      public Int32 MaxStoppedServers { get; private set; }
      public FarmState State { get; private set; }

      public WebFarm(ConfigurationElement el, Int32 minServersAvailableForDegraded = 1)
      {
         Name = el.GetAttributeValue("name").ToString();
         Enabled = Boolean.Parse(el.GetAttributeValue("enabled").ToString());
         //Int32 minServers = -1;
         //if (Int32.TryParse(el.GetAttribute("minimumServers").ToString(), out minServers))
         //   MinimumServers = minServers;
         //
         //Int32 maxStoppedServers = -1;
         //if (Int32.TryParse(el.GetAttribute("maximumStoppedServers").ToString(), out maxStoppedServers))
         //   MaxStoppedServers = maxStoppedServers;

         Servers = new List<Server>();
         foreach (var server in el.GetCollection())
            Servers.Add(new Server(server));

         Int32 healthyServers = Servers.Where(s => s.Healthy).Count();
         if (healthyServers <= minServersAvailableForDegraded) State = FarmState.Degraded;
         else if (healthyServers == 0) State = FarmState.Failed;
         else State = FarmState.Normal;
      }

      public static IEnumerable<WebFarm> GetWebfarms(Int32 minServersAvailableForDegraded = 1)
      {
         var serverManager = new ServerManager();
         var webFarmCollection = serverManager.GetApplicationHostConfiguration().GetSection("webFarms").GetCollection();
         if (webFarmCollection.Count == 0)
            yield break;

         foreach (var item in webFarmCollection)
            yield return new WebFarm(item, minServersAvailableForDegraded);

      }
      public static WebFarm GetWebFarm(String farmName)
      {
         return GetWebfarms().Where(f => f.Name == farmName).FirstOrDefault();
      }
   }
}

