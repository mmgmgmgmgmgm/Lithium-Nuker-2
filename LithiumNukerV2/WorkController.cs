using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LithiumNukerV2
{
    public class WorkController
    {
        public List<List<T>> Seperate<T>(List<T> items, int loadCount)
        {
            var loads = new List<List<T>>();

            for (int x = 0; x < loadCount; x++)
                loads.Add(new List<T>());
            for (int x = 0; x < items.Count; x++)
                loads[x % loadCount].Add(items[x]);

            return loads;
        }
    }
}
