# UnitMan.cs - simple API for UOM Conversion

[![License: MS-PL](https://img.shields.io/badge/license-MS--PL-yellowgreen.svg)](https://opensource.org/licenses/MS-PL)  

* [Source Code](https://github.com/mbeloshapkin/UnitMan/blob/master/UnitMan.cs)
* [API Reference](https://mbeloshapkin.github.io/UnitMan/api/AeroGIS.Common.UnitMan.html)

## Concept

### Simplicity of API

UnitMan is simplest (I hope) possible API for UOM conversion. It consists of single C# class. The code may looks as below.  
```csharp
var UM = new UnitMan();
...
XmlDocument xUoms = new XmlDocument();
xUoms.Load("MyUOMDefinitions.xml");
UM.Load("xUoms");
...
double psi = UM.Convert(pressure_Kgcm2, "kg1cm-2", "lb1in-2");`
```

### Automatic calculation of conversion coefficients

You no need to define all the conversion coefficients. UnitMan can calculate that automatically. In the example above you no need to define what is pressure. You no need to define how much pounds per square inch in one kilogram per square meter. You just need to define how much inches in one meter and how much pounds in one kilogramm. The conversion coefficient for pressure will be calculated automatically

### UOM Signatures

UnitMan API does not define any classes, records or object for UOM representation. UOMs are completely described by it's text UOM signature.   

## UOM Definition files

UnitMan loads UOM definitions from XML documents. Root node of the document must have some mandatory attributes.

```xml
<UnitSystem Version="2.0" ISO639Code = "ENA" Description = "Some readable description" UOMRegEx = "[A-Z,a-z]">
```
* Version - format version. Must be 2.0
* ISO639Code - language code which defines the UI culture of user. Single application may operate for users of different UI cultures and more than one definition file could be loaded by host application
* Description - brief description of subject to which the system of meaurement could be used. Or something similar to that. For example, "nuclear phisics", "astronomy", "construction", etc.
* UOMRegEx - the regex which defines letters, which can be used for UOM label, excluding digits. The latin letters are usually enough, until you don't use greek letters or oother national alphabets.    

### Master UOMS and UOM Domains

# Free Flexible UOM Converter

FF Converter is just a code example for UnitMan API. Nevertheless, it is fully functional application and it could be handy in practice. If you need for simple and flexible UOM converter then download it here and install. Define your own unit system using sample XML UOM definition files as templates. 
Add any UOMs in any languages and drop to trash UOM definitions that looks useless for you.

![screenshot](https://github.com/mbeloshapkin/UnitMan/blob/master/Img/FFC-Screenshot.png?raw=true)
