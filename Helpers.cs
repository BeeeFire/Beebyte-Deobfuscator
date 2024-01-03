using Beebyte_Deobfuscator.Lookup;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator
{
    class Helpers
    {
        public static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || (pattern.Trim().Length == 0))
            {
                return false;
            }

            try
            {
                Regex.Match("", pattern);
            }
            catch (System.ArgumentException)
            {
                return false;
            }

            return true;
        }

        public static float CompareFieldOffsets(LookupType t1, LookupType t2, LookupModule lookupModel)
        {
            if (t1.Il2CppType == null || t2.Il2CppType == null)
            {
                return 1.0f;
            }

            float comparative_score = 1.0f;

            float score_penalty = comparative_score / t1.Fields.Count(f => !f.IsStatic && !f.IsLiteral);

            foreach (var f1 in t1.Fields.Select((Value, Index) => new { Value, Index }))
            {
                if (t2.Fields.Count <= f1.Index)
                {
                    continue;
                }

                LookupField f2 = t2.Fields[f1.Index];
                if (f1.Value.Name == f2.Name)
                {
                    return 1.5f;
                }

                if (!Regex.Match(f1.Value.Name, lookupModel.NamingRegex).Success && f1.Value.Name != f2.Name)
                {
                    return 0.0f;
                }

                if (f1.Value.IsStatic || f1.Value.IsLiteral)
                {
                    continue;
                }

                if (f1.Value.Offset != f2.Offset)
                {
                    comparative_score -= score_penalty;
                }
            }

            return comparative_score;
        }

        public static float CompareFieldTypes(LookupType t1, LookupType t2, LookupModule lookupModel)
        {
            float comparative_score = 1.0f;
            float score_diff = 0;
            int all_cnt = t1.Fields.Count();
            int total = t1.Fields.Count(f => !f.IsStatic && !f.IsLiteral);

            if (total > 0)
                score_diff = comparative_score / total;
            else
            {
                if (all_cnt > 0)
                    score_diff = comparative_score / all_cnt;
                else
                    score_diff = 0;
            }

            foreach (var f1 in t1.Fields.Select((Value, Index) => new { Value, Index }))
            {
                if (t2.Fields.Count <= f1.Index)
                {
                    continue;
                }

                LookupField f2 = t2.Fields[f1.Index];
                if (f1.Value.Name == f2.Name)
                {
                    comparative_score += score_diff;
                    continue;
                    //return 1.5f;

                }
                if (!Regex.Match(f2.Name, lookupModel.NamingRegex).Success && f1.Value.Name != f2.Name)
                {
                    return 0.0f;
                }

                if (f1.Value.IsStatic || f1.Value.IsLiteral)
                {
                    if (f1.Value.Il2CppField.DefaultValue != null && (f1.Value.Il2CppField.DefaultValue.Equals(f2.Il2CppField.DefaultValue)))
                    {
                        comparative_score += score_diff;
                        continue;
                        //return 1.5f;
                    }
                }
                else
                {
                    if (f1.Value.Il2CppField.FieldType.Equals(f2.Il2CppField.FieldType))
                    {
                        comparative_score += score_diff;
                        continue;
                    }
                }

                if (f1.Value.GetType().Namespace == "System" && f2.GetType().Namespace == "System" && f1.Value.Name == f2.Name)
                {
                    comparative_score -= score_diff;
                }
            }

            return comparative_score;
        }

        public static string SanitizeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }
    }
}
