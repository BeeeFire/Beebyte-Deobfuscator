using Beebyte_Deobfuscator.Lookup;
using Beebyte_Deobfuscator.Output.Generators;
using Il2CppInspector.PluginAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Beebyte_Deobfuscator.Output
{
    public enum TranslationType
    {
        TypeTranslation,
        FieldTranslation,
        MethodTranslation
    }

    public enum ExportType
    {
        None,
        PlainText,
        Classes,
        JsonTranslations,
        JsonMappings
    }

    public interface IGenerator
    {
        public abstract void Generate(LookupModule module);

        public static IGenerator GetGenerator(ExportType exportType)
        {
            return exportType switch
            {
                ExportType.Classes => new Il2CppTranslatorGenerator(),
                ExportType.PlainText => new PlaintextTranslationsGenerator(),
                ExportType.JsonTranslations => new JsonTranslationsGenerator(),
                ExportType.JsonMappings => new JsonMappingGenerator(),
                _ => null,
            };
        }
    }

    public class Translation : IEquatable<Translation>
    {
        public readonly TranslationType Type;
        public string ObfName;
        public string CleanName;

        public LookupField _field;
        public LookupType _type;
        public LookupMethod _method;

        public Translation(string obfName, LookupType type)
        {
            ObfName = obfName;
            CleanName = type.Name;
            _type = type;
            Type = TranslationType.TypeTranslation;
        }

        public Translation(string obfName, string cleanName, LookupType type)
        {
            ObfName = obfName;
            CleanName = cleanName;
            _type = type;
            Type = TranslationType.TypeTranslation;
        }

        public Translation(string obfName, LookupField field)
        {
            ObfName = obfName;
            CleanName = field.Name;
            _field = field;
            Type = TranslationType.FieldTranslation;
        }

        public Translation(string obfName, LookupMethod method)
        {
            ObfName = obfName;
            CleanName = method.Name;
            _method = method;
            Type = TranslationType.MethodTranslation;
        }

        public static void Export(LookupModule lookupModule, ExportType exportType)
        {
            Console.WriteLine("Generating output..");
            if (!lookupModule.Translations.Any(t => t.CleanName != t.ObfName))
            {
                return;
            }

            List<Translation> filteredTranslations = lookupModule.Translations
                .Where(t => !t.CleanName.EndsWith('&'))
                .GroupBy(t => t.CleanName)
                .Select(t => t.First())
                .GroupBy(t => t.ObfName)
                .Select(t => t.First())
                .ToList();
            lookupModule.Translations.Clear();
            lookupModule.Translations.AddRange(filteredTranslations);


            IGenerator.GetGenerator(exportType).Generate(lookupModule);
        }

        public bool Equals([AllowNull] Translation other)
        {
            return other.CleanName == CleanName && other.ObfName == ObfName;
        }
        public override string ToString()
        {
            return $"{ObfName}/{CleanName}";
        }
    }
}
