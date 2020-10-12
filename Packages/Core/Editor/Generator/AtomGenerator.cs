using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityAtoms.Editor
{
    [CreateAssetMenu(fileName = "AtomGenerator", menuName = "Unity Atoms/Generation/AtomGenerator", order = 0)]
    public class AtomGenerator : ScriptableObject
    {
        public string FullQualifiedName;
        public string Namespace;
        public string BaseType;

        public int GenerationOptions;

        // Referencing Generated Files here:
        public List<MonoScript> Scripts = new List<MonoScript>(AtomTypes.ALL_ATOM_TYPES.Count);

        public void Generate()
        {
            var type = Type.GetType($"{FullQualifiedName}");
            if (type == null) throw new TypeLoadException($"Type could not be found ({FullQualifiedName})");
            var isValueTypeEquatable = type.GetInterfaces().Contains(typeof(IEquatable<>));

            var templateVariables = Generator.CreateTemplateVariablesMap(BaseType, Namespace, "BaseAtoms");
            var capitalizedValueType = BaseType.Capitalize();
            var templates = Generator.GetTemplatePaths();
            var templateConditions =
                Generator.CreateTemplateConditions(isValueTypeEquatable, Namespace, "BaseAtoms", BaseType);
            var baseWritePath =
                Path.Combine((Path.GetDirectoryName(AssetDatabase.GetAssetPath(this.GetInstanceID()))) ?? "Assets/",
                    "Generated");

            Directory.CreateDirectory(baseWritePath);

            Scripts.Clear();
            var t = GenerationOptions;
            var idx = 0;
            while (t > 0)
            {
                if (t % 2 == 1)
                {
                    var atomType = AtomTypes.ALL_ATOM_TYPES[idx];

                    templateVariables["VALUE_TYPE_NAME"] =
                        atomType.IsValuePair ? $"{capitalizedValueType}Pair" : capitalizedValueType;
                    var valueType = atomType.IsValuePair ? $"{capitalizedValueType}Pair" : BaseType;
                    templateVariables["VALUE_TYPE"] = valueType;

                    var resolvedRelativeFilePath = Templating.ResolveVariables(templateVariables: templateVariables,
                        toResolve: atomType.RelativeFileNameAndPath);
                    var targetPath = Path.Combine(baseWritePath, resolvedRelativeFilePath);

                    var newCreated = !File.Exists(targetPath);

                    Generator.Generate(new AtomReceipe(atomType, valueType), baseWritePath, templates,
                        templateConditions, templateVariables);

                    if (newCreated) AssetDatabase.ImportAsset(targetPath);
                    var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(targetPath);
                    Scripts.Add(ms);
                }
                else
                {
                    Scripts.Add(null);
                }

                idx++;
                t >>= 1;
            }
        }
    }
}
