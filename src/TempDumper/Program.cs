using System;
using System.Reflection;

try
{
    var assembly = Assembly.GetAssembly(typeof(Microsoft.FluentUI.AspNetCore.Components.FluentDialog));
    if (assembly == null) return;

    var types = new[] {
        "Microsoft.FluentUI.AspNetCore.Components.FluentSelect`2",
        "Microsoft.FluentUI.AspNetCore.Components.FluentTextArea"
    };

    foreach (var typeName in types)
    {
        var type = assembly.GetType(typeName);
        if (type == null)
        {
            // Try without generic suffix if needed
            type = assembly.GetType(typeName.Replace("`2", ""));
        }

        if (type == null)
        {
            Console.WriteLine($"Type {typeName} not found.");
            continue;
        }

        Console.WriteLine($"\n--- Properties of {type.FullName} ---");
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.Name.Contains("Value") || prop.Name.Contains("Selected"))
            {
                Console.WriteLine($"Property: {prop.PropertyType.Name} {prop.Name}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.ToString());
}
