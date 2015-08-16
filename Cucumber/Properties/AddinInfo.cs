using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"Cucumber", 
	Namespace = "Cucumber",
	Version = "1.0"
)]

[assembly:AddinName ("Cucumber")]
[assembly:AddinCategory ("Language Binding")]
[assembly:AddinDescription ("Adds support for cucumber projects.")]
[assembly:AddinAuthor ("ItsVeryWindy")]

