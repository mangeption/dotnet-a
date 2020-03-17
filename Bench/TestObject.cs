

using System;
using Microsoft.EntityFrameworkCore;

namespace bench
{
    public class TestObjectContext: DbContext
    {
        public TestObjectContext(DbContextOptions<TestObjectContext> options): base(options)
        {

        }
        public DbSet<TestObject> TestObjects {get; set;}
    }
    public class TestObject
    {
        public TestObject()
        {
            ID = Guid.NewGuid();
            Title = ID.ToString();
        }
        public Guid ID {get; set;}
        public string Title {get; set;}
    }
}