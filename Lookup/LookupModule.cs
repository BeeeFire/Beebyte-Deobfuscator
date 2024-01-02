using Beebyte_Deobfuscator.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModule
    {
        public List<Translation> Translations { get; } = new List<Translation>();
        public LookupModel ObfModule { get; private set; }
        public LookupModel CleanModule { get; private set; }

        private LookupMatrix Matrix = new LookupMatrix();
        private Dictionary<LookupType, LookupType> Matches = new Dictionary<LookupType, LookupType>();
        private HashSet<LookupType> MatchedTypes = new HashSet<LookupType>();
        public List<string> CleanTypeNames { get; } = new List<string>();
        public SortedDictionary<string, LookupType> CleanTypes { get; } = new SortedDictionary<string, LookupType>();
        public List<string> ObfTypeNames { get; } = new List<string>();
        public SortedDictionary<string, LookupType> ObfTypes { get; } = new SortedDictionary<string, LookupType>();

        public string NamingRegex;

        public LookupModule(string namingRegex) => NamingRegex = namingRegex;

        public void Init(LookupModel obfModule, LookupModel cleanModule, EventHandler<string> statusCallback = null)
        {
            ObfModule = obfModule;
            CleanModule = cleanModule;

            statusCallback?.Invoke(this, "Sorting obfuscated and un-obfsucated types");
            foreach (LookupType type in cleanModule.Types)
            {
                if (type.IsEmpty)
                {
                    continue;
                }
                if (!CleanTypes.ContainsKey(type.Name))
                {
                    CleanTypes.Add(type.Name, type);
                }
            }
            foreach (LookupType type in obfModule.Types)
            {
                if (type.IsEmpty)
                {
                    continue;
                }
                if (!ObfTypes.ContainsKey(type.Name))
                {
                    ObfTypes.Add(type.Name, type);
                }
            }

            CleanTypeNames.AddRange(cleanModule.Types.Where(x => !x.IsEmpty).Select(x => x.Name));
            ObfTypeNames.AddRange(obfModule.Types.Where(x => !x.IsEmpty).Select(x => x.Name));

            int current = 0;
            int total = ObfTypes.Count(t => t.Value.ShouldTranslate(this) && t.Value.DeclaringType.IsEmpty);
            foreach (var type in ObfTypes.Where(t => t.Value.ShouldTranslate(this) && t.Value.DeclaringType.IsEmpty))
            {
                statusCallback?.Invoke(this, $"Created {current}/{total} Lookup Matrices");

                Matrix.Insert(type.Value);
            }
        }

        public LookupType GetMatchingType(LookupType type, bool checkoffsets)
        {
            LookupType typeInfo = null;

            List<LookupType> types = Matrix.Get(type);
            if (types.Count() == 1 && types[0] != null)
            {
                bool t1 = !MatchedTypes.Contains(types[0]);
                bool t2 = !CleanTypeNames.Contains(types[0].Name) || types[0].Fields.Any(f => Regex.IsMatch(f.Name, NamingRegex));
                if (t1 && t2)
                {
                    if (type.Namespace != types[0].Namespace)
                    {
                        return null;
                    }
                    else if (type.Methods.Count > 0 &&
                     (type.Methods.Count - (type.Methods.Count * 0.3) > types[0].Methods.Count ||
                         type.Methods.Count + (type.Methods.Count * 0.3) < types[0].Methods.Count))
                    {
                        return null;
                    }

                    MatchedTypes.Add(types[0]);
                    Matches.Add(types[0], type);
                    return types[0];
                }
            }

            if (types.Count() > 10)
            {
                checkoffsets = true;
            }

            float best_score = 0.0f;
            foreach (LookupType t in types)
            {
                if (MatchedTypes.Contains(t) || (CleanTypeNames.Contains(t.Name) && !t.Fields.Any(f => Regex.IsMatch(f.Name, NamingRegex))))
                {
                    continue;
                }

                if (t.Name == type.Name)
                {
                    typeInfo = t;
                    break;
                }
                float score = 0.0f;

                if (checkoffsets)
                {
                    score = (Helpers.CompareFieldOffsets(t, type, this) + Helpers.CompareFieldTypes(t, type, this)) / 2;
                }
                else
                {
                    score = Helpers.CompareFieldTypes(t, type, this);
                }

                if (score > best_score)
                {
                    best_score = score;
                    typeInfo = t;
                }
            }

            if (typeInfo != null && !MatchedTypes.Contains(typeInfo))
            {
                // Namespace Check
                if (type.Namespace != typeInfo.Namespace)
                {
                    return null;
                }
                // Method Count Check
                else if (type.Methods.Count > 0 &&
                        (type.Methods.Count - (type.Methods.Count * 0.3) > typeInfo.Methods.Count ||
                            type.Methods.Count + (type.Methods.Count * 0.3) < typeInfo.Methods.Count))
                {
                    return null;
                }

                Matches.Add(typeInfo, type);
                MatchedTypes.Add(typeInfo);
            }
            return typeInfo;
        }


        //public LookupMethod GetMatchingMethod(LookupMethod cleanMethod, List<LookupMethod> obfMethods, bool checkoffsets)
        //{
        //    obfMethods = obfMethods.Where(
        //        m => (m.ParameterList.Count == cleanMethod.ParameterList.Count && m.ReturnType == cleanMethod.ReturnType && !m.IsPropertymethod)
        //        ).ToList();

        //    return cleanMethod;
        //}

        public void TranslateTypes(bool checkoffsets = false, EventHandler<string> statusCallback = null)
        {
            //var filteredTypes = CleanTypes.Where(t => t.Value.DeclaringType.IsEmpty && !t.Value.IsEnum && !Regex.Match(t.Value.Name, @"\+<.*(?:>).*__[1-9]{0,4}|[A-z]*=.{1,4}|<.*>").Success);
            var filteredTypes = CleanTypes.Where(t => t.Value.DeclaringType.IsEmpty && !Regex.Match(t.Value.Name, @"\+<.*(?:>).*__[1-9]{0,4}|[A-z]*=.{1,4}|<.*>").Success);
            int total = filteredTypes.Count();
            int current = 0;
            foreach (var type in filteredTypes)
            {
                current++;
                //if (type.Value.Name == "ChatDefine")
                //{
                //    Console.WriteLine("Test2");
                //}

                LookupType matchingType = GetMatchingType(type.Value, checkoffsets);

                if (matchingType == null)
                {
                    continue;
                }

                if (matchingType.Children.Any() && type.Value.Children.Any())
                {
                    LookupTranslators.TranslateChildren(matchingType, type.Value, checkoffsets, this);
                }

                if (matchingType.Il2CppType.IsNested && matchingType.IsNested)
                {
                    Console.WriteLine("123123");
                }

                matchingType.SetName(type.Key, this);
            }
            TranslateFields(checkoffsets);

            // Find method name
            // Matches key : obf, value : clean
            foreach (var type in Matches)
            {
                foreach (var cleanMethod in type.Value.Methods)
                {
                    List<LookupMethod> machingMethods = type.Key.Methods.Where(
                        m => (m.ParameterList.Count == cleanMethod.ParameterList.Count
                        && m.ReturnType.CSharpName == cleanMethod.ReturnType.CSharpName
                        && m.Il2CppMethod.IsStatic == cleanMethod.Il2CppMethod.IsStatic
                        && m.Il2CppMethod.IsVirtual == cleanMethod.Il2CppMethod.IsVirtual
                        && m.Il2CppMethod.IsGenericMethod == cleanMethod.Il2CppMethod.IsGenericMethod
                        && m.Il2CppMethod.IsPublic == cleanMethod.Il2CppMethod.IsPublic
                        && m.Il2CppMethod.IsPrivate == cleanMethod.Il2CppMethod.IsPrivate
                        && !m.IsPropertymethod)
                    ).ToList();

                    if (machingMethods.Count == 0)
                    {
                        continue;
                    }
                    else if (machingMethods.Count == 1 && machingMethods[0] != null)
                    {
                        machingMethods[0].SetName(cleanMethod.Name, NamingRegex);
                    }
                    else
                    {
                        List<LookupMethod> foundMethods = new List<LookupMethod>();
                        int successCnt = cleanMethod.ParameterList.Count;

                        foreach (var eachMethod in machingMethods)
                        {
                            int totalCnt = 0;

                            // iterate index with key, value
                            foreach (var eachParam in eachMethod.ParameterList.Select((Value, Index) => new { Value, Index }))
                            {
                                if (eachParam.Value.Name != cleanMethod.ParameterList[eachParam.Index].Name)
                                {
                                    continue;
                                }
                                totalCnt++;
                            }

                            if (successCnt == totalCnt)
                            {
                                foundMethods.Add(eachMethod);
                            }
                        }

                        if (foundMethods.Count == 1)
                        {
                            Translations.Add(new Translation(foundMethods[0].Name, cleanMethod));
                            foundMethods[0].SetName(cleanMethod.Name, NamingRegex);
                        }
                        else
                            Console.WriteLine("Test");
                    }
                }
                statusCallback?.Invoke(this, $"Deobfuscated {current}/{total} types");
            }

            //TranslateFields(checkoffsets);
        }

        public void TranslateFields(bool checkoffsets)
        {
            // Translate fields for matched types
            foreach (var match in Matches)
            {
                LookupTranslators.TranslateFields(match.Key, match.Value, checkoffsets, this);
            }

            // Translate fields for types with the same name
            foreach (var cleanType in CleanTypes.Where(t => ObfTypes.ContainsKey(t.Value.Name)))
            {
                LookupType obfType = ObfTypes[cleanType.Key];
                if (!MatchedTypes.Contains(cleanType.Value) && obfType != null)
                {
                    Translations.Add(new Translation(cleanType.Key, cleanType.Value));
                    LookupTranslators.TranslateFields(obfType, cleanType.Value, checkoffsets, this);
                }
            }
        }
    }
}
