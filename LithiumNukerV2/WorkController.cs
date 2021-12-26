using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LithiumNukerV2
{
    public class WorkController
    {
        public List<List<dynamic>> Seperate(List<dynamic> items, int loadCount)
        {
            var loads = new List<List<dynamic>>();

            for (int x = 0; x < loadCount; x++)
                loads.Add(new List<dynamic>());
            for (int x = 0; x < items.Count; x++)
                loads[x % loadCount].Add(items[x]);

            return loads;
        }

        public List<List<dynamic>> Seperate(List<dynamic> items)
        {
            return Seperate(items, Settings.Threads);
        }
    }
}
