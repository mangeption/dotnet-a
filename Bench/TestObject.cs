

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace bench
{
    public class TestObjectContext: DbContext
    {
        public TestObjectContext(DbContextOptions<TestObjectContext> options): base(options)
        {

        }
        public TestObjectContext()
        {
           
        }
        public DbSet<TestObject> TestObjects {get; set;}
    }

    public class TestObjectContextFactory : IDesignTimeDbContextFactory<TestObjectContext>
    {
        const string connectionString = "server=localhost;port=3306;user=test;database=test;password=test";

        public TestObjectContext CreateDbContext(string[] args)
        {
             var optionsBuilder = new DbContextOptionsBuilder<TestObjectContext>();
            optionsBuilder.UseMySql(connectionString);

            return new TestObjectContext(optionsBuilder.Options);
        }
    }
    public class TestObject
    {
        public TestObject()
        {
            ID = Guid.NewGuid();
            T1 = ID.ToString();
            T2 = ID.ToString();
            T3 = ID.ToString();
            T4 = ID.ToString();
            T5 = ID.ToString();
            T6 = ID.ToString();
            T7 = ID.ToString();
            T8 = ID.ToString();
            T9 = ID.ToString();
        }
        public Guid ID {get; set;}
        public string T1 {get; set;}
        public string T2 {get; set;}
        public string T3 {get; set;}
        public string T4 {get; set;}
        public string T5 {get; set;}
        public string T6 {get; set;}
        public string T7 {get; set;}
        public string T8 {get; set;}
        public string T9 {get; set;}
    }
}