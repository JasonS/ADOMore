namespace ADOMore.Test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ReflectionExtensionsTests
    {
        [Test]
        public void UtilityIsDatabaseCompatible()
        {
            IEnumerable<Type> types = typeof(TestClass).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanWrite && p.Name.StartsWith("Set", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.PropertyType);

            foreach (Type type in types)
            {
                Assert.IsTrue(type.IsDatabaseCompatible());
            }

            Assert.IsFalse(typeof(TestClass).IsDatabaseCompatible());
            Assert.IsFalse(typeof(int[]).IsDatabaseCompatible());
            Assert.IsFalse(typeof(IDictionary).IsDatabaseCompatible());
            Assert.IsFalse(typeof(object).IsDatabaseCompatible());
        }

        [Test]
        public void UtilityUnderlyingType()
        {
            Assert.AreEqual(typeof(bool), typeof(bool).UnderlyingType());
            Assert.AreEqual(typeof(long), typeof(long?).UnderlyingType());
            Assert.AreEqual(typeof(List<int>), typeof(List<int>).UnderlyingType());
        }
    }
}