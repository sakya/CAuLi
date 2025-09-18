using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Utility;

/// <summary>
/// A generic config value
/// </summary>
[XmlRoot("ConfigValue")]
public class GenericConfigValue
{
  public GenericConfigValue()
  {

  }

  /// <summary>
  /// The config value name
  /// </summary>
  [XmlAttribute]
  public string Name { get; set; }

  /// <summary>
  /// The config value value
  /// </summary>
  [XmlElement]
  public string Value { get; set; }
} // GenericConfigValue

/// <summary>
/// A Generic configuration containing Name-Value data
/// </summary>
[XmlRoot("GenericConfig")]
public class GenericConfig
{
  public GenericConfig()
  {
    Values = new List<GenericConfigValue>();
  }

  #region Properties
  /// <summary>
  /// Configuration name
  /// </summary>
  [XmlAttribute]
  public string Name { get; set; }

  /// <summary>
  /// Configuration values
  /// </summary>
  [XmlArray("Values")]
  [XmlArrayItem("ConfigValue")]
  public List<GenericConfigValue> Values { get; set; }
  #endregion

  #region Public operations
  /// <summary>
  /// Get a <see cref="GenericConfigValue"/> by name
  /// </summary>
  /// <param name="name">The value name</param>
  /// <returns>A <see cref="GenericConfigValue"/> or null</returns>
  public GenericConfigValue GetConfigValue(string name)
  {
    if (Values != null) {
      foreach (GenericConfigValue v in Values) {
        if (v.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
          return v;
      }
    }
    return null;
  } // GetConfigValue

  /// <summary>
  /// Get the value of a <see cref="GenericConfigValue"/>.
  /// <para>If the <see cref="GenericConfigValue"/> cannot be found the defaultValue is returned</para>
  /// </summary>
  /// <param name="name">The value name</param>
  /// <param name="defaultValue">The default value</param>
  /// <returns>The configuration value</returns>
  public string GetValue(string name, string defaultValue)
  {
    GenericConfigValue v = GetConfigValue(name);
    if (v != null)
      return v.Value;
    return defaultValue;
  } // GetValue

  public void SetValue(string name, string value)
  {
    GenericConfigValue v = GetConfigValue(name);
    if (v != null) {
      v.Value = value;
    }else {
      Values.Add(new GenericConfigValue() { Name = name, Value = value });
    }
  } // SetValue
  #endregion
} // GenericConfig