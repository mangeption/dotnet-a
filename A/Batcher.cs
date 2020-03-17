using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;

namespace A
{
    public class Batcher<T>
    {
        private readonly string _tableName = typeof(T).Name;
        private PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        private int _batchSize = 20;
        private int _colCount = 0;
        private bool _onDuplicate = false;
        private int _counter = 0;
        private MySqlCommand _command;

        private StringBuilder builder;
        private string[] _placeHolders;

        private const int _numPlaceHolders = 200;

        public Batcher(int batchSize, bool onDuplicate = false)
        {
            _batchSize = batchSize;
            _colCount = _properties.Length;
            _onDuplicate = onDuplicate;
            _placeHolders = _generatePlaceHolders(_numPlaceHolders);

            _command = new MySqlCommand(_buildInsertQuery(batchSize, _colCount));
        }

        public Batcher(MySqlConnection _conn, int batchSize, bool onDuplicate = false) : this(batchSize, onDuplicate)
        {
            _command.Connection = _conn;
        }

        public Batcher(MySqlTransaction transaction, int batchSize, bool onDuplicate = false) : this(batchSize, onDuplicate)
        {
            _command.Connection = transaction.Connection;
            _command.Transaction = transaction;
        }


        public string query { get { return _command.CommandText; } }

        public MySqlTransaction transaction
        {
            set
            {
                _command.Connection = value.Connection;
                _command.Transaction = value;
            }
        }

        public MySqlConnection connection { set { _command.Connection = value; } }

        public void Insert(T entry)
        {
            var idx = _counter * _colCount;
            foreach (var property in _properties)
            {
                // var propertyName = $"@{idx+1}";
                var propertyName = _getPlaceHolderAt(idx);
                _command.Parameters.AddWithValue(propertyName, property.GetValue(entry));
                idx++;
            }
            _counter++;

            if (_counter == _batchSize)
            {
                _command.ExecuteNonQuery();
                _reset();
            }
        }

        public void Flush()
        {
            if (_counter <= 0) return;

            var query = _buildInsertQuery(_counter, _colCount);
            using (var command = new MySqlCommand(query, _command.Connection, _command.Transaction))
            {
                var paramEnumerator = _command.Parameters.GetEnumerator();
                while (paramEnumerator.MoveNext())
                {
                    command.Parameters.Add(paramEnumerator.Current);
                }

                command.ExecuteNonQuery();
                _reset();
            }
        }

        private string _buildInsertQuery(int rowCount, int colCount)
        {
            var builder = new StringBuilder();
            builder.Append("insert into ");
            builder.Append(_tableName);
            builder.Append("s values "); // Table name is the plural form of class name

            for (int i = 1; i <= rowCount; i++)
            {
                var startIdx = (i - 1) * colCount + 1;
                builder.Append(_buildRow(startIdx, colCount));
                builder.Append(",");
            }

            builder.Length--;

            if (_onDuplicate)
            {
                builder.Append(_buildOnDuplicate(colCount));
            }

            return builder.ToString();
        }

        private string _buildOnDuplicate(int colCount)
        {
            var builder = new StringBuilder();
            // Only for MySQL 8.0.19 and higher
            // For lower version, please refer to https://dev.mysql.com/doc/refman/8.0/en/insert-on-duplicate.html
            builder.Append(" as r on duplicate key update ");
            for (int i = 0; i < colCount; i++)
            {
                builder.Append($"{_properties[i].Name}=r.{_properties[i].Name},");
            }
            builder.Length--;
            return builder.ToString();
        }

        private string _buildRow(int startIdx, int colCount)
        {
            var builder = new StringBuilder();
            builder.Append("(");
            for (int j = startIdx; j < colCount + startIdx; j++)
            {
                builder.Append($"@{j},");
            }
            builder.Length--;
            builder.Append(")");
            return builder.ToString();
        }

        private void _reset()
        {
            _command.Parameters.Clear();
            _counter = 0;
        }

        private string[] _generatePlaceHolders(int n)
        {
            var placeHolders = new string[n];
            for (int i = 0; i < n; i++)
            {
                placeHolders[i] = $"@{i+1}";
            }

            return placeHolders;
        }

        private string _getPlaceHolderAt(int i)
        {
            if (i < _numPlaceHolders)
            {
                return _placeHolders[i];
            }

            return $"@{i+1}";
        }
    }
}