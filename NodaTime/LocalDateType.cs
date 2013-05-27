using System;
using System.Data;
using NHibernate.SqlTypes;
using NHibernate.Type;
using NodaTime;

namespace NHibernate.NodaTime
{
    public class LocalDateType : PrimitiveType
    {
        public LocalDateType() : base(SqlTypeFactory.Date)
        {
        }

        public override string Name
        {
            get { return "LocalDate"; }
        }

        public override System.Type ReturnedClass
        {
            get { return typeof(LocalDate); }
        }

        public override void Set(IDbCommand cmd, object value, int index)
        {
            var parm = cmd.Parameters[index] as IDataParameter;
            if (value == null)
                parm.Value = DBNull.Value;
            else
            {
                var localDate = (LocalDate)value;
                parm.DbType = DbType.Date;
                parm.Value = DateTime.SpecifyKind(new DateTime(localDate.Year, localDate.Month, localDate.Day),
                                                  DateTimeKind.Local);
            }
        }

        public override object Get(IDataReader rs, int index)
        {
            try
            {
                var dateTime = Convert.ToDateTime(rs[index]);
                return new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);
            }
            catch (Exception ex)
            {
                throw new FormatException(string.Format("Input string '{0}' was not in the correct format.", rs[index]), ex);
            }
        }

        public override object Get(IDataReader rs, string name)
        {
            return Get(rs, rs.GetOrdinal(name));
        }

        public override object FromStringValue(string xml)
        {
            var dateTime = DateTime.Parse(xml);
            return new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public override string ObjectToSQLString(object value, Dialect.Dialect dialect)
        {
            return ((LocalDate)value).ToString("'yyyyMMdd'", null);
        }

        public override System.Type PrimitiveClass
        {
            get { return typeof(LocalDate); }
        }

        public override object DefaultValue
        {
            get { return default(LocalDate); }
        }

        public override bool IsEqual(object x, object y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;

            return ((LocalDate)x).Equals(y);
        }

        public override int GetHashCode(object x, EntityMode entityMode)
        {
            var date = (LocalDate)x;
            var hashCode = 1;
            unchecked
            {
                hashCode = 31 * hashCode + date.Day;
                hashCode = 31 * hashCode + date.Month;
                hashCode = 31 * hashCode + date.Year;
            }
            return hashCode;
        }

        public override string ToString(object val)
        {
            return ((LocalDate) val).ToString("d", null);
        }
    }
}