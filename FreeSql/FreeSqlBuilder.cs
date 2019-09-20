﻿using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using FreeSql.DataAnnotations;
using FreeSql.Internal;

namespace FreeSql
{
    public class FreeSqlBuilder
    {
        DataType _dataType;
        string _masterConnectionString;
        string[] _slaveConnectionString;
        bool _isAutoSyncStructure = false;
        bool _isSyncStructureToLower = false;
        bool _isSyncStructureToUpper = false;
        bool _isConfigEntityFromDbFirst = false;
        bool _isNoneCommandParameter = false;
        bool _isLazyLoading = false;
        StringConvertType _entityPropertyConvertType = StringConvertType.None;
        Action<DbCommand> _aopCommandExecuting = null;
        Action<DbCommand, string> _aopCommandExecuted = null;
        Type _providerType = null;

        /// <summary>
        /// 使用连接串
        /// </summary>
        /// <param name="dataType">数据库类型</param>
        /// <param name="connectionString">数据库连接串</param>
        /// <param name="providerType">提供者的类型，一般不需要指定，如果一直提示“缺少 FreeSql 数据库实现包：FreeSql.Provider.MySql.dll，可前往 nuget 下载”的错误，说明反射获取不到类型，此时该参数可排上用场</param>
        /// <returns></returns>
        public FreeSqlBuilder UseConnectionString(DataType dataType, string connectionString, Type providerType = null)
        {
            _dataType = dataType;
            _masterConnectionString = connectionString;
            _providerType = providerType;
            return this;
        }
        /// <summary>
        /// 使用从数据库，支持多个
        /// </summary>
        /// <param name="slaveConnectionString">从数据库连接串</param>
        /// <returns></returns>
        public FreeSqlBuilder UseSlave(params string[] slaveConnectionString)
        {
            _slaveConnectionString = slaveConnectionString;
            return this;
        }
        /// <summary>
        /// 【开发环境必备】自动同步实体结构到数据库，程序运行中检查实体表是否存在，然后创建或修改
        /// </summary>
        /// <param name="value">true:运行时检查自动同步结构, false:不同步结构</param>
        /// <returns></returns>
        public FreeSqlBuilder UseAutoSyncStructure(bool value)
        {
            _isAutoSyncStructure = value;
            return this;
        }
        /// <summary>
        /// 转小写同步结构
        /// </summary>
        /// <param name="value">true:转小写, false:不转</param>
        /// <returns></returns>
        public FreeSqlBuilder UseSyncStructureToLower(bool value)
        {
            _isSyncStructureToLower = value;
            return this;
        }
        /// <summary>
        /// 转大写同步结构
        /// </summary>
        /// <param name="value">true:转大写, false:不转</param>
        /// <returns></returns>
        public FreeSqlBuilder UseSyncStructureToUpper(bool value)
        {
            _isSyncStructureToUpper = value;
            return this;
        }
        /// <summary>
        /// 使用数据库的主键和自增，适用 DbFirst 模式，无须在实体类型上设置 [Column(IsPrimary)] 或者 ConfigEntity。此功能目前可用于 mysql/sqlserver/postgresql。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FreeSqlBuilder UseConfigEntityFromDbFirst(bool value)
        {
            _isConfigEntityFromDbFirst = value;
            return this;
        }
        /// <summary>
        /// 不使用命令参数化执行，针对 Insert/Update，也可临时使用 IInsert/IUpdate.NoneParameter() 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FreeSqlBuilder UseNoneCommandParameter(bool value)
        {
            _isNoneCommandParameter = value;
            return this;
        }
        /// <summary>
        /// 延时加载导航属性对象，导航属性需要声明 virtual
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public FreeSqlBuilder UseLazyLoading(bool value)
        {
            _isLazyLoading = value;
            return this;
        }
        /// <summary>
        /// 监视数据库命令对象
        /// </summary>
        /// <param name="executing">执行前</param>
        /// <param name="executed">执行后，可监视执行性能</param>
        /// <returns></returns>
        public FreeSqlBuilder UseMonitorCommand(Action<DbCommand> executing, Action<DbCommand, string> executed = null)
        {
            _aopCommandExecuting = executing;
            _aopCommandExecuted = executed;
            return this;
        }

        /// <summary>
        /// 自动转换实体属性名称 Entity Property -> Db Filed
        /// <para></para>
        /// *不会覆盖 [Column] 特性设置的Name
        /// </summary>
        /// <param name="convertType"></param>
        /// <returns></returns>
        public FreeSqlBuilder UseEntityPropertyNameConvert(StringConvertType convertType)
        {
            _entityPropertyConvertType = convertType;
            return this;
        }

        public IFreeSql Build() => Build<IFreeSql>();
        public IFreeSql<TMark> Build<TMark>()
        {
            if (string.IsNullOrEmpty(_masterConnectionString)) throw new Exception("参数 masterConnectionString 不可为空，请检查 UseConnectionString");
            IFreeSql<TMark> ret = null;
            var type = _providerType;
            if (type?.IsGenericType == true) type = type.MakeGenericType(typeof(TMark));
            if (type == null)
            {
                switch (_dataType)
                {
                    case DataType.MySql:
                        type = Type.GetType("FreeSql.MySql.MySqlProvider`1,FreeSql.Provider.MySql")?.MakeGenericType(typeof(TMark));
                        if (type == null) type = Type.GetType("FreeSql.MySql.MySqlProvider`1,FreeSql.Provider.MySqlConnector")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.MySql.dll，可前往 nuget 下载");
                        break;
                    case DataType.SqlServer:
                        type = Type.GetType("FreeSql.SqlServer.SqlServerProvider`1,FreeSql.Provider.SqlServer")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.SqlServer.dll，可前往 nuget 下载");
                        break;
                    case DataType.PostgreSQL:
                        type = Type.GetType("FreeSql.PostgreSQL.PostgreSQLProvider`1,FreeSql.Provider.PostgreSQL")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.PostgreSQL.dll，可前往 nuget 下载");
                        break;
                    case DataType.Oracle:
                        type = Type.GetType("FreeSql.Oracle.OracleProvider`1,FreeSql.Provider.Oracle")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Oracle.dll，可前往 nuget 下载");
                        break;
                    case DataType.Sqlite:
                        type = Type.GetType("FreeSql.Sqlite.SqliteProvider`1,FreeSql.Provider.Sqlite")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Sqlite.dll，可前往 nuget 下载");
                        break;

                    case DataType.OdbcOracle:
                        type = Type.GetType("FreeSql.Odbc.Oracle.OdbcOracleProvider`1,FreeSql.Provider.Odbc")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Odbc.dll，可前往 nuget 下载");
                        break;
                    case DataType.OdbcSqlServer:
                        type = Type.GetType("FreeSql.Odbc.SqlServer.OdbcSqlServerProvider`1,FreeSql.Provider.Odbc")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Odbc.dll，可前往 nuget 下载");
                        break;
                    case DataType.OdbcMySql:
                        type = Type.GetType("FreeSql.Odbc.MySql.OdbcMySqlProvider`1,FreeSql.Provider.Odbc")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Odbc.dll，可前往 nuget 下载");
                        break;
                    case DataType.OdbcPostgreSQL:
                        type = Type.GetType("FreeSql.Odbc.PostgreSQL.OdbcPostgreSQLProvider`1,FreeSql.Provider.Odbc")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Odbc.dll，可前往 nuget 下载");
                        break;
                    case DataType.Odbc:
                        type = Type.GetType("FreeSql.Odbc.Default.OdbcProvider`1,FreeSql.Provider.Odbc")?.MakeGenericType(typeof(TMark));
                        if (type == null) throw new Exception("缺少 FreeSql 数据库实现包：FreeSql.Provider.Odbc.dll，可前往 nuget 下载");
                        break;

                    default: throw new Exception("未指定 UseConnectionString");
                }
            }
            ret = Activator.CreateInstance(type, new object[] { _masterConnectionString, _slaveConnectionString }) as IFreeSql<TMark>;
            if (ret != null)
            {
                ret.CodeFirst.IsAutoSyncStructure = _isAutoSyncStructure;

                ret.CodeFirst.IsSyncStructureToLower = _isSyncStructureToLower;
                ret.CodeFirst.IsSyncStructureToUpper = _isSyncStructureToUpper;
                ret.CodeFirst.IsConfigEntityFromDbFirst = _isConfigEntityFromDbFirst;
                ret.CodeFirst.IsNoneCommandParameter = _isNoneCommandParameter;
                ret.CodeFirst.IsLazyLoading = _isLazyLoading;
                var ado = ret.Ado as Internal.CommonProvider.AdoProvider;
                ado.AopCommandExecuting += _aopCommandExecuting;
                ado.AopCommandExecuted += _aopCommandExecuted;

                //添加实体属性名全局AOP转换处理
                if (_entityPropertyConvertType != StringConvertType.None)
                {
                    switch (_entityPropertyConvertType)
                    {
                        case StringConvertType.Lower:
                            ret.Aop.ConfigEntityProperty += (_, e) =>
                                e.ModifyResult.Name = e.Property.Name.ToLower();
                            break;
                        case StringConvertType.Upper:
                            ret.Aop.ConfigEntityProperty += (_, e) =>
                                e.ModifyResult.Name = e.Property.Name.ToUpper();
                            break;
                        case StringConvertType.PascalCaseToUnderscore:
                            ret.Aop.ConfigEntityProperty += (_, e) =>
                                e.ModifyResult.Name = StringUtils.PascalCaseToUnderScore(e.Property.Name);
                            break;
                        case StringConvertType.PascalCaseToUnderscoreWithLower:
                            ret.Aop.ConfigEntityProperty += (_, e) =>
                                e.ModifyResult.Name = StringUtils.PascalCaseToUnderScore(e.Property.Name).ToLower();
                            break;
                        case StringConvertType.PascalCaseToUnderscoreWithUpper:
                            ret.Aop.ConfigEntityProperty += (_, e) =>
                                e.ModifyResult.Name = StringUtils.PascalCaseToUnderScore(e.Property.Name).ToUpper();
                            break;
                        default:
                            break;
                    }
                }
                //处理 MaxLength
                ret.Aop.ConfigEntityProperty += new EventHandler<Aop.ConfigEntityPropertyEventArgs>((s, e) =>
                {
                    object[] attrs = null;
                    try
                    {
                        attrs = e.Property.GetCustomAttributes(false).ToArray(); //.net core 反射存在版本冲突问题，导致该方法异常
                    }
                    catch { }

                    var maxlenAttr = attrs.Where(a => {
                        return ((a as Attribute)?.TypeId as Type)?.Name == "MaxLengthAttribute";
                    }).FirstOrDefault();
                    if (maxlenAttr != null)
                    {
                        var lenProp = maxlenAttr.GetType().GetProperties().Where(a => a.PropertyType.IsNumberType()).FirstOrDefault();
                        if (lenProp != null && int.TryParse(string.Concat(lenProp.GetValue(maxlenAttr, null)), out var tryval))
                        {
                            if (tryval != 0)
                            {
                                switch (ret.Ado.DataType)
                                {
                                    case DataType.MySql:
                                    case DataType.OdbcMySql:
                                        e.ModifyResult.DbType = tryval > 0 ? $"varchar({tryval})" : "text";
                                        break;
                                    case DataType.SqlServer:
                                    case DataType.OdbcSqlServer:
                                        e.ModifyResult.DbType = tryval > 0 ? $"nvarchar({tryval})" : "nvarchar(max)";
                                        break;
                                    case DataType.PostgreSQL:
                                    case DataType.OdbcPostgreSQL:
                                        e.ModifyResult.DbType = tryval > 0 ? $"varchar({tryval})" : "text";
                                        break;
                                    case DataType.Oracle:
                                    case DataType.OdbcOracle:
                                        e.ModifyResult.DbType = tryval > 0 ? $"nvarchar2({tryval})" : "nvarchar2(4000)";
                                        break;
                                    case DataType.Sqlite:
                                        e.ModifyResult.DbType = tryval > 0 ? $"nvarchar({tryval})" : "text";
                                        break;

                                }
                            }
                        }
                    }
                });
            }

            return ret;
        }
    }
}
