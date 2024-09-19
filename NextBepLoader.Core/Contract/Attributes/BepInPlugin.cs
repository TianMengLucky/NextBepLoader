using System;
using System.Linq;
using AsmResolver.DotNet;
using Version = SemanticVersioning.Version;

namespace NextBepLoader.Core.Contract.Attributes;

/// <summary>
///     This attribute denotes that a class is a plugin, and specifies the required metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BepInPlugin : Attribute
{
    public BepInPlugin() {}
    
    /// <param name="GUID">The unique identifier of the plugin. Should not change between plugin versions.</param>
    /// <param name="Name">The user friendly name of the plugin. Is able to be changed between versions.</param>
    /// <param name="Version">The specific version of the plugin.</param>
    public BepInPlugin(string GUID, string Name, string Version)
    {
        this.GUID = GUID;
        this.Name = Name;
        this.Version = TryParseLongVersion(Version);
    }

    /// <summary>
    ///     The unique identifier of the plugin. Should not change between plugin versions.
    /// </summary>
    public string GUID { get; protected set; }


    /// <summary>
    ///     The user friendly name of the plugin. Is able to be changed between versions.
    /// </summary>
    public string Name { get; protected set; }


    /// <summary>
    ///     The specific version of the plugin.
    /// </summary>
    public Version? Version { get; protected set; }

    private static Version? TryParseLongVersion(string version)
    {
        if (Version.TryParse(version, out var v))
            return v;

        // no System.Version.TryParse() on .NET 3.5
        try
        {
            var longVersion = new System.Version(version);

            return new Version(longVersion.Major, longVersion.Minor,
                               longVersion.Build != -1 ? longVersion.Build : 0);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    internal static BepInPlugin? FromCecilType(TypeDefinition td)
    {
        var attr = MetadataHelper.GetCustomAttributes<BepInPlugin>(td, false).FirstOrDefault();

        if (attr == null)
            return null;

        var args = attr.Signature!.NamedArguments;

        return new BepInPlugin((string)args[0].Argument.Element!,
                               (string)args[1].Argument.Element!,
                               (string)args[2].Argument.Element!);
    }
}