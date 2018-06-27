using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.persistence
{

    public interface QueryLoader
    {
        string LoadByName(string queryName);
        string LoadByName(string queryName, int index);
        string LoadByParams(IDictionary<string, object> parameters);
    }

}
