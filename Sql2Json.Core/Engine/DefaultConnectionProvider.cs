using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sql2Json.Core.Engine
{
    public class DefaultConnectionProvider : IConnectionProvider
    {
        private Func<IDbConnection> newConnectionFunc;

        public DefaultConnectionProvider(Func<IDbConnection> newConnectionFunc)
        {
            if (newConnectionFunc == null) throw new ArgumentNullException("newConnectionFunc cant be null");
            this.newConnectionFunc = newConnectionFunc;
        }


        public IDbConnection GetNewConnection()
        {
            return newConnectionFunc();
        }
    }
}
