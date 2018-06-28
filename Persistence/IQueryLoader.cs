using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.persistence
{

    /// <summary>
    /// It models a component that gets a SQL query as a string
    /// </summary>
    public interface IQueryLoader
    {
        /// <summary>
        /// Loads the SQL string by an assigned query name
        /// </summary>
        /// <param name="queryName">The logic query name</param>
        /// <returns>The loaded SQL string</returns>
        string LoadByName(string queryName);

        /// <summary>
        /// Loads the SQL string by an assigned query name and an index.
        /// The index can be useful to scan a data structure containing many SQL queries.
        /// </summary>
        /// <param name="queryName">The logic query name</param>
        /// <param name="index">An index to find the query in a specialized data structure (a dictionary or a collection)</param>
        /// <returns>The loaded SQL string</returns>
        string LoadByName(string queryName, int index);

        /// <summary>
        /// Loads the SQL string basing on a generic parameters dictionary
        /// </summary>
        /// <param name="parameters">The search criteria as a dictionary</param>
        /// <returns>The loaded SQL string</returns>
        string LoadByParams(IDictionary<string, object> parameters);
    }

}
