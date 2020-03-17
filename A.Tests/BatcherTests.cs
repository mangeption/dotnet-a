using System;
using Xunit;
using A;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace Batcher.Tests
{
    public class TestObject : IComparable<TestObject>
    {
        public TestObject()
        {
            ID = Guid.NewGuid();
            A = "abc";
            B = 1;
            C = 2;
        }

        public TestObject(string a, int b, int c)
        {
            ID = Guid.NewGuid();
            A = a;
            B = b;
            C = c;
        }
        public Guid ID { get; set; }
        public string A { get; set; }
        public int B { get; set; }
        public int C { get; set; }

        public int CompareTo(TestObject target) => ID.CompareTo(target.ID);
    }

    public class TestObjectContext : DbContext
    {
        public TestObjectContext(DbContextOptions<TestObjectContext> options) : base(options) { }
        public DbSet<TestObject> TestObjects { get; set; }
    }



    public class BatcherTest : IDisposable
    {
        private MySqlConnection _conn;
        private const string connectionString = "server=localhost;port=3306;user=test;database=test;password=test";
        private IConfiguration configuration;
        public BatcherTest()
        {

            _conn = new MySqlConnection(connectionString);
            _conn.Open();
            var query = "create table if not exists TestObjects(id varchar(36) primary key, a text, b int, c int)";
            using (var cmd = new MySqlCommand(query, _conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        [Fact]
        public void TestBuildInsertQuery()
        {
            var batcher = new Batcher<TestObject>(3);
            Assert.Equal("insert into TestObjects values (@1,@2,@3,@4),(@5,@6,@7,@8),(@9,@10,@11,@12)", batcher.query);
        }

        [Fact]
        public void TestBuildInsertQueryOnDuplicate()
        {
            var batcher = new Batcher<TestObject>(3, true);
            Assert.Equal("insert into TestObjects values (@1,@2,@3,@4),(@5,@6,@7,@8),(@9,@10,@11,@12) as r on duplicate key update ID=r.ID,A=r.A,B=r.B,C=r.C", batcher.query);
        }

        [Fact]
        public void TestInsert_3_Objects_Batchsize_3()
        {
            var transact = _conn.BeginTransaction();
            var batcher = new Batcher<TestObject>(transact, 3);
            var o1 = new TestObject();
            var o2 = new TestObject();
            var o3 = new TestObject();
            batcher.Insert(o1);
            batcher.Insert(o2);
            batcher.Insert(o3);
            batcher.Flush();
            transact.Commit();

            var context = _newContext(_conn);
            var results = context.TestObjects.FromSqlRaw("select * from TestObjects").ToList();
            results.Sort();

            var x = new List<TestObject>();
            x.Add(o1);
            x.Add(o2);
            x.Add(o3);
            x.Sort();
            Assert.Equal(x, results);
        }

        [Fact]
        public void TestInsert_2_Objects_Batchsize_3()
        {
            var transact = _conn.BeginTransaction();
            var batcher = new Batcher<TestObject>(transact, 3);
            var o1 = new TestObject();
            var o2 = new TestObject();
            var o3 = new TestObject();
            batcher.Insert(o1);
            batcher.Insert(o2);
            batcher.Flush();
            transact.Commit();

            var context = _newContext(_conn);
            var results = context.TestObjects.FromSqlRaw("select * from TestObjects").ToList();
            results.Sort();

            var x = new List<TestObject>();
            x.Add(o1);
            x.Add(o2);
            x.Sort();
            Assert.Equal(x, results);
        }

        [Fact]
        public void TestInsert_3_Objects_Batchsize_3_onDuplicate()
        {
            var context = _newContext(_conn);
            var o1 = new TestObject();
            o1.A = "hesitation";
            context.TestObjects.Add(o1);
            context.SaveChanges();

            var curO1 = context.TestObjects.Find(o1.ID);
            Assert.Equal("hesitation", curO1.A);

            var o2 = new TestObject();
            var o3 = new TestObject();

            var batcher = new Batcher<TestObject>(_conn, 3, true);
            o1.A = "is_defeat";
            batcher.Insert(o1);
            batcher.Insert(o2);
            batcher.Insert(o3);
            batcher.Flush();

            var results = context.TestObjects.FromSqlRaw("select * from TestObjects").ToArray();
            Array.Sort(results);
            var x = new TestObject[] { o1, o2, o3 };
            Array.Sort(x);
            Assert.Equal(x, results);

            curO1 = context.TestObjects.Find(o1.ID);
            Assert.Equal("is_defeat", curO1.A);
        }

        private TestObjectContext _newContext(MySqlConnection conn)
        {
            var optionBuilder = new DbContextOptionsBuilder<TestObjectContext>();
            optionBuilder.UseMySql(conn);

            return new TestObjectContext(optionBuilder.Options);
        }

        public void Dispose()
        {
            var query = "drop table TestObjects";
            using (var cmd = new MySqlCommand(query, _conn))
            {
                cmd.ExecuteNonQuery();
            }
            _conn.Close();
            _conn.Dispose();
        }

    }
}
