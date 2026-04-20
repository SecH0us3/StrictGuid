using System;
using StrictGuid.Library;

namespace StrictGuid.Demo
{
    public enum EntityType : byte
    {
        User = 1,
        Order = 2
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- StrictGuid Demo ---");

            // 1. Generation
            Guid userId = EntityType.User.NewStrictGuid();
            Guid orderId = EntityType.Order.NewStrictGuid(StrictGuidEntropy.Secure);

            Console.WriteLine($"Generated User ID (Fast):   {userId}");
            Console.WriteLine($"Generated Order ID (Secure): {orderId}");

            // 2. Extraction
            var type = userId.GetEntityType<EntityType>();
            Console.WriteLine($"Extracted type from userId: {type}");

            // 3. Validation
            try
            {
                Console.WriteLine("\nValidating orderId against EntityType.Order...");
                orderId.ValidateEntityType(EntityType.Order);
                Console.WriteLine("Success: Validation passed.");

                Console.WriteLine("\nValidating userId against EntityType.Order (should fail)...");
                userId.ValidateEntityType(EntityType.Order);
            }
            catch (StrictGuidException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Expected Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
