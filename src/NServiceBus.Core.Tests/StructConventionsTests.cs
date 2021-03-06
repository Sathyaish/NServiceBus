namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using ApprovalTests;
    using NUnit.Framework;

    [TestFixture]
    public class StructConventionsTests
    {
        [Test]
        public void ApproveStructsWhichDontFollowStructGuidelines()
        {
            var approvalBuilder = new StringBuilder();
            approvalBuilder.AppendLine(@"-------------------------------------------------- REMEMBER --------------------------------------------------
CONSIDER defining a struct instead of a class if instances of the type are small and commonly short-lived or are commonly embedded in other objects.

AVOID defining a struct unless the type has all of the following characteristics:
   * It logically represents a single value, similar to primitive types(int, double, etc.).
   * It has an instance size under 16 bytes.
   * It is immutable.
   * It will not have to be boxed frequently.

In all other cases, you should define your types as classes.
-------------------------------------------------- REMEMBER --------------------------------------------------
");

            var assembly = typeof(Endpoint).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsValueType || type.IsEnum || type.IsSpecialName|| type.Namespace == null || !type.Namespace.StartsWith("NServiceBus") || type.FullName.Contains("__")) continue;

                var violatedRules = new List<string> { $"{type.FullName} violates the following rules:" };

                InspectSizeOfStruct(type, violatedRules);
                InspectWhetherStructViolatesMutabilityRules(type, violatedRules);

                if (violatedRules.Count <= 1) continue;
                foreach (var violatedRule in violatedRules)
                {
                    approvalBuilder.AppendLine(violatedRule);
                }
                approvalBuilder.AppendLine();
            }

            Approvals.Verify(approvalBuilder.ToString());
        }

        static void InspectWhetherStructViolatesMutabilityRules(Type type, List<string> violatedRules)
        {
            InspectWhetherStructContainsReferenceTypes(type, violatedRules);
            InspectWhetherStructContainsPublicFields(type, violatedRules);
            InspectWhetherStructContainsWritableProperties(type, violatedRules);
        }

        static void InspectWhetherStructContainsReferenceTypes(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following fields are reference types, which are potentially mutable:" };

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.FieldType == typeof(string) && (fieldInfo.IsInitOnly || fieldInfo.IsLiteral))
                {
                    continue;
                }

                if (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsInterface)
                {
                    mutabilityRules.Add($"      - Field {fieldInfo.Name} of type { fieldInfo.FieldType } is a reference type.");
                }
            }

            if(mutabilityRules.Count > 1)
                violatedRules.AddRange(mutabilityRules);
        }

        static void InspectWhetherStructContainsPublicFields(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following fields are public, so the type is not immutable:" };

            var fields = type.GetFields();

            foreach (var fieldInfo in fields)
            {
                if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral)
                {
                    mutabilityRules.Add($"      - Field {fieldInfo.Name} of type { fieldInfo.FieldType } is public.");
                }
            }

            if (mutabilityRules.Count > 1)
                violatedRules.AddRange(mutabilityRules);
        }

        static void InspectWhetherStructContainsWritableProperties(Type type, List<string> violatedRules)
        {
            var mutabilityRules = new List<string> { "   - The following properties can be written to, so the type is not immutable:" };

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    mutabilityRules.Add($"      - Property {property.Name} of type { property.PropertyType } can be written to.");
                }
            }

            if (mutabilityRules.Count > 1)
                violatedRules.AddRange(mutabilityRules);
        }

        static void InspectSizeOfStruct(Type type, List<string> violatedRules)
        {
            try
            {
                var sizeOf = Marshal.SizeOf(type);
                if (IsLargerThanSixteenBytes(sizeOf))
                {
                    violatedRules.Add($"   - The size is {sizeOf} bytes, which exceeds the recommended maximum of 16 bytes.");
                }
            }
            catch (Exception)
            {
                violatedRules.Add("   - The size cannot be determined. This type likely violates all struct rules.");
            }
        }

        static bool IsLargerThanSixteenBytes(int sizeOf)
        {
            return sizeOf > 16;
        }
    }
}