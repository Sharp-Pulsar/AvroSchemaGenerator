# AvroSchemaGenerator
Use to generate Avro Schema

## Getting Started
Install the NuGet package [AvroSchemaGenerator](https://www.nuget.org/packages/AvroSchemaGenerator/) and copy/paste the code below 

```csharp
public class Course
    {
        public string Level { get; set; }
        
        public int Year { get; set; }
        
        public string State { get; set; }
        
        public string Gender { get; set; }
    }
```

By default, `AvroSchemaGenerator` generates schema with optional fields. The code below is an example of how to mark fields as required

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
public class Course
    {
        [Required]
        public string Level { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string State { get; set; }
        
        [Required]
        public string Gender { get; set; }
    }
```

You can assign a  default value as well
```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
public class Course
    {
        [DefaultValue("200")]
        [Required]
        public string Level { get; set; }

        [Required]
        public int Year { get; set; }

        [DefaultValue("Closed")]
        public string State { get; set; }

        public string Gender { get; set; }
    }
```

### Known issue
Recursive fields! 
`AvroSchemaGenerator` handles that by throwing `StackOverflowException` early!
