using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Sql2Json.Core.Engine
{
    /// <summary>
    /// Command Parameter Setter Default implementation
    /// </summary>
    public class DefaultCommandWithParameterSetter : ICommandWithParameterSetter
    {
        const string PREFIX_LOOKUP_COLUMN = "column:";
        const string PREFIX_LOOKUP_CONTEXT = "context:";
        const string PLACEHOLDER_PATTERN = "\\$\\{.+?\\}";
        const string PARAM_PREFIX = "p";
        private IDbCommand command;
        private IDbConnection connection;
        internal List<Func<ParentObject, IDictionary<string, object>, object>> parameterExpressions = new List<Func<ParentObject, IDictionary<string, object>, object>>();
        private CommandParameterPrefix prefix;

        /// <summary>
        /// Creates a Command Parameter Setter.
        /// The placeholders ${column:_} and ${context:_} will be replaced by real command parameter like @myParameter.
        /// The parameters will be assigned during execution with parent row column values or values from the context
        /// </summary>
        /// <param name="sqlText">sql text to execute</param>
        /// <param name="connection">Connection which is used to create the command objects</param>
        /// <param name="prefix">Command paramenter prefix types</param>
        public DefaultCommandWithParameterSetter(string sqlText, IConnectionProvider connectionProvider, CommandParameterPrefix prefix)
        {
            this.prefix = prefix;
            connection = connectionProvider.GetNewConnection();
            connection.Open();
            command = connection.CreateCommand();
            var processedSqlText = ParseParameterExpression(sqlText, this.parameterExpressions, prefix, command);
            command.CommandText = processedSqlText;
            command.Prepare();
        }
        internal string ParseParameterExpression(string sqlText, List<Func<ParentObject, IDictionary<string, object>, object>> parsedParameterExpression, CommandParameterPrefix prefix, IDbCommand dbCommand)
        {
            var regExpMatcher = new Regex(PLACEHOLDER_PATTERN, RegexOptions.IgnoreCase);
            return regExpMatcher.Replace(sqlText, match => ReplaceParameter(match, parsedParameterExpression, prefix, command));
        }

        internal string ReplaceParameter(Match match, List<Func<ParentObject, IDictionary<string, object>, object>> parsedParameterExpression, CommandParameterPrefix prefix, IDbCommand dbCommand)
        {
            var newParameterIndex = parsedParameterExpression.Count;
            var expression = RemovePlaceholderPattern(match.Value);
            string dbTypeName = null;
            if (expression.StartsWith(PREFIX_LOOKUP_COLUMN))
            {
                var parts = ParseDbTypeName(expression.Remove(0, PREFIX_LOOKUP_COLUMN.Length));
                dbTypeName = parts.Item2;
                parsedParameterExpression.Add(CreateLookUpFunc(parts.Item1));
            }
            if (expression.StartsWith(PREFIX_LOOKUP_CONTEXT))
            {
                var parts = ParseDbTypeName(expression.Remove(0, PREFIX_LOOKUP_CONTEXT.Length));
                dbTypeName = parts.Item2;
                parsedParameterExpression.Add(CreateContextLookUpFunc(parts.Item1));
            }

            var paramName = prefix.ToRealPrefix() + PARAM_PREFIX + newParameterIndex;
            CreateDbParameterAndAssignValue(dbCommand, null, paramName, dbTypeName);
            return paramName;
        }

        internal Tuple<string, string> ParseDbTypeName(string expression)
        {
            if (expression == null) return null;
            var parts = expression.Split(':');
            if (parts.Length == 2)
            {
                return new Tuple<string, string>(parts[0], parts[1]);
            }
            else
            {
                return new Tuple<string, string>(expression, null);
            }
        }

        internal Func<ParentObject, IDictionary<string, object>, object> CreateLookUpFunc(string columnName)
        {
            return (ParentObject parent, IDictionary<string, object> context) => parent.Values[columnName];
        }

        internal Func<ParentObject, IDictionary<string, object>, object> CreateContextLookUpFunc(string key)
        {
            return (ParentObject parent, IDictionary<string, object> context) => context[key];
        }

        internal string RemovePlaceholderPattern(string paramWithPlaceholder)
        {
            if (paramWithPlaceholder.Length > 3)
            {
                return paramWithPlaceholder.Substring(2, paramWithPlaceholder.Length - 3);
            }
            return paramWithPlaceholder;
        }
        /// <summary>
        /// Prepare the command for the current parent object and context.
        /// This will set all command parameters according to parameter setup
        /// </summary>
        /// <param name="parentObject">Current parent object</param>
        /// <param name="context">current Context</param>
        /// <returns>The prepared command with assigned command parameters</returns>
        public IDbCommand Prepare(ParentObject parentObject, IDictionary<string, object> context)
        {
            for (int i = 0; i < parameterExpressions.Count; i++)
            {
                object value = parameterExpressions[i](parentObject, context);
                CreateDbParameterAndAssignValue(command, value, prefix.ToRealPrefix() + PARAM_PREFIX + i, null);
            }
            return command;
        }

        internal void CreateDbParameterAndAssignValue(IDbCommand command, object value, string parameterName, string dbTypeName)
        {
            var parameterIndex = command.Parameters.IndexOf(parameterName);
            if (parameterIndex < 0)
            {
                var newParameter = command.CreateParameter();
                newParameter.ParameterName = parameterName;
                if (!string.IsNullOrEmpty(dbTypeName))
                    newParameter.DbType = (DbType)Enum.Parse(typeof(DbType), dbTypeName);
                newParameter.Value = value;
                command.Parameters.Add(newParameter);
            }
            else
            {
                var existingParameter = command.Parameters[parameterIndex];
                (existingParameter as IDbDataParameter).Value = value;
            }
        }
        /// <summary>
        /// Dispose the actual command object and clear all expression
        /// </summary>
        public void Dispose()
        {
            command.Dispose();
            parameterExpressions.Clear();
            connection.Dispose();
        }


    }
}
