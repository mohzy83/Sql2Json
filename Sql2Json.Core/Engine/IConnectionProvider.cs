using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Sql2Json.Core.Engine
{
    public interface IConnectionProvider
    {
        IDbConnection GetNewConnection();
    }
}
