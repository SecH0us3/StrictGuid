using Xunit;
using StrictGuid.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrictGuid.Tests
{
    public enum TestEntity : byte
    {
        // Service types
        Log = 0,
        WebHook = 1,
        Request = 2,
        Response = 3,
        Trace = 4,

        // Domain entities
        User = 5,
        Order = 6,
        Product = 7,
        Category = 8,
        Customer = 9,
        Invoice = 10,
        Payment = 11,
        Shipment = 12,
        Review = 13,
        Comment = 14,
        Address = 15,
        Cart = 16,
        Wishlist = 17,
        Promotion = 18,
        Coupon = 19,
        Setting = 20,
        Notification = 21,
        AuditLog = 22,
        Permission = 23,
        Role = 24,
        Task = 25,
        Project = 26,
        Article = 27,
        Tag = 28,
        File = 29,
        Folder = 30,
        Message = 31,
        Chat = 32,
        Ticket = 33,
        Report = 34,
        Subscription = 35,
        Transaction = 36,
        
        SystemInternal = 255
    }

    public enum InvalidEnum : int
    {
        Value = 1
    }

    public class StrictGuidTests
    {
        [Fact]
        public void NewStrictGuid_ShouldEmbedCorrectType()
        {
            var id = TestEntity.User.NewStrictGuid();
            var type = id.GetEntityType<TestEntity>();
            
            Assert.Equal(TestEntity.User, type);
        }

        [Fact]
        public void AllEntityTypes_ShouldNotOverlap()
        {
            var values = Enum.GetValues<TestEntity>();
            var generatedIds = new Dictionary<TestEntity, Guid>();

            // Generate ID for each type
            foreach (var expectedType in values)
            {
                var id = expectedType.NewStrictGuid();
                generatedIds.Add(expectedType, id);
            }

            // Verify that each extracted type matches the original
            foreach (var (expectedType, id) in generatedIds)
            {
                var actualType = id.GetEntityType<TestEntity>();
                Assert.Equal(expectedType, actualType);
            }
        }

        [Fact]
        public void AllEntityTypes_CrossValidation_ShouldFail()
        {
            var allTypes = Enum.GetValues<TestEntity>();

            foreach (var typeA in allTypes)
            {
                // Generate a GUID for type A
                var idA = typeA.NewStrictGuid();

                foreach (var typeB in allTypes)
                {
                    // If types are different, validation should fail
                    if (typeA != typeB)
                    {
                        Assert.Throws<StrictGuidException>(() => idA.ValidateEntityType(typeB));
                        Assert.False(idA.IsEntityType(typeB), $"ID of type {typeA} should not be identified as {typeB}");
                    }
                    else
                    {
                        // If types are the same, validation should pass
                        idA.ValidateEntityType(typeB);
                        Assert.True(idA.IsEntityType(typeB));
                    }
                }
            }
        }

        [Fact]
        public void ValidateEntityType_ShouldThrow_OnMismatch()
        {
            var id = TestEntity.User.NewStrictGuid();
            Assert.Throws<StrictGuidException>(() => id.ValidateEntityType(TestEntity.SystemInternal));
        }

        [Fact]
        public void NewStrictGuid_ShouldThrow_WhenEnumIsNotByte()
        {
            Assert.Throws<StrictGuidException>(() => InvalidEnum.Value.NewStrictGuid());
        }

        [Fact]
        public void GeneratedGuid_ShouldBeVersion8()
        {
            var id = TestEntity.User.NewStrictGuid();
            var bytes = id.ToByteArray(bigEndian: true);
            
            int version = (bytes[6] & 0xF0) >> 4;
            Assert.Equal(8, version);
        }

        [Fact]
        public void Sequential_ShouldBeOrdered()
        {
            var ids = new List<Guid>();
            for (int i = 0; i < 50; i++)
            {
                ids.Add(TestEntity.User.NewStrictGuid(StrictGuidEntropy.Sequential));
            }

            // To properly verify the sequence of GUIDv7/v8, 
            // they need to be compared as byte arrays (lexicographical byte-order)
            for (int i = 0; i < ids.Count - 1; i++)
            {
                var bytes1 = ids[i].ToByteArray(bigEndian: true);
                var bytes2 = ids[i+1].ToByteArray(bigEndian: true);
                
                // Verify monotonicity: a new GUID cannot have a timestamp older than the previous one
                for (int j = 0; j < 6; j++)
                {
                    if (bytes2[j] > bytes1[j]) break; // Order is correct, time has increased
                    if (bytes2[j] < bytes1[j]) 
                        Assert.Fail($"GUID at {i+1} ({ids[i+1]}) is older than {i} ({ids[i]}) in byte {j}");
                    
                    // If bytes are equal, continue checking until the end of the 6-byte time prefix
                }
            }
        }

        [Fact]
        public void Sequential_ShouldPreserveType()
        {
            var id = TestEntity.SystemInternal.NewStrictGuid(StrictGuidEntropy.Sequential);
            Assert.Equal(TestEntity.SystemInternal, id.GetEntityType<TestEntity>());
        }

        [Theory]
        [InlineData(StrictGuidEntropy.Fast)]
        [InlineData(StrictGuidEntropy.Secure)]
        [InlineData(StrictGuidEntropy.Sequential)]
        public void AllEntropySources_ShouldWork(StrictGuidEntropy source)
        {
            var id = TestEntity.User.NewStrictGuid(source);
            Assert.Equal(TestEntity.User, id.GetEntityType<TestEntity>());
        }

        [Fact]
        public void StandardV4Guid_ShouldNotMatchAnyType()
        {
            var v4 = Guid.NewGuid();
            // TestEntity.Log = 0. A v4 Guid should not be mistakenly identified as type 0.
            Assert.False(v4.IsEntityType(TestEntity.Log));
        }
    }
}