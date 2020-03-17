using BenchmarkDotNet.Attributes;
using A;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;


namespace bench
{
    [MemoryDiagnoser]
    public class BatcherBench
    {
        const string connectionString = "server=localhost;port=3306;user=test;database=test;password=test";
        TestObject[] _objects;
        MySqlConnection _conn;
        public BatcherBench()
        {
            _conn = new MySqlConnection(connectionString);
            _conn.Open();
              using (var cmd = new MySqlCommand("truncate TestObjects", _conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _objects = _spawn(1000);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // using (MySqlConnection conn = new MySqlConnection(connectionString))
            // {
            //     conn.Open();
            //     using (var cmd = new MySqlCommand("truncate TestObjects", conn))
            //     {
            //         cmd.ExecuteNonQuery();
            //     }
            //     conn.Close();
            // }

            using (var cmd = new MySqlCommand("truncate TestObjects", _conn))
            {
                cmd.ExecuteNonQuery();
            }

        }


        [Benchmark(Baseline = true)]
        public void EFInsertLoopTransact()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                _EFInsertLoopTransact(conn);
                conn.Close();
            }

            // _EFInsertLoopTransact(_conn);
        }

        private void _EFInsertLoopTransact(MySqlConnection conn)
        {
            var context = _newContext(conn);

            using (var tx = context.Database.BeginTransaction())
            {
                foreach (TestObject o in _objects)
                {
                    context.Add(o);
                }
                context.SaveChanges();
                tx.Commit();
            }

        }

        [Benchmark]
        public void EFInsertRangeTransact()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                _EFInsertRangeTransact(conn);
                conn.Close();
            }
            // _EFInsertRangeTransact(_conn);

        }

        private void _EFInsertRangeTransact(MySqlConnection conn)
        {
            var context = _newContext(conn);

            using (var tx = context.Database.BeginTransaction())
            {
                context.AddRange(_objects);
                context.SaveChanges();
                tx.Commit();
            }

        }

        [Benchmark]
        public void BatcherInsertTransact()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                _BatcherInsertTransact(conn);
                conn.Close();
            }

            // _BatcherInsertTransact(_conn);
        }

        private void _BatcherInsertTransact(MySqlConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                var batcher = new Batcher<TestObject>(tx, 100);
                foreach (TestObject o in _objects)
                {
                    batcher.Insert(o);
                }
                batcher.Flush();
                tx.Commit();

            }
        }
        private TestObject[] _spawn(int n)
        {
            TestObject[] os = new TestObject[n];
            for (int i = 0; i < n; i++)
            {
                os[i] = new TestObject();
            }
            return os;
        }

        private TestObjectContext _addWithContext(TestObjectContext context, MySqlConnection conn, MySqlTransaction tx, TestObject o, int count, int commitCount)
        {
            context.Add(o);
            if (count % commitCount == 0)
            {
                context.SaveChanges();
                context.Dispose();
                var newContext = _newContext(conn);
                newContext.Database.UseTransaction(tx);

                return newContext;
            }

            return context;
        }

        private TestObjectContext _newContext(MySqlConnection conn)
        {
            var optionBuilder = new DbContextOptionsBuilder<TestObjectContext>();
            optionBuilder.UseMySql(conn);
            
            var context = new TestObjectContext(optionBuilder.Options);
            return context;
        }


    }
}