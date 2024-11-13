    using System.Reflection;
    using System.Runtime.CompilerServices;

    class ClassManager
    {
        public static bool IsArrayDynamic(string arrayName) 
        {
            if (Enum.TryParse(arrayName, out Globals.DynamicArrayTypes.DynamicArrayList dResult) && Enum.IsDefined(typeof(Globals.DynamicArrayTypes.DynamicArrayList), dResult)) return true;
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUPropertyStatic(string propertyName) 
        {
            if (Enum.TryParse(propertyName, out Globals.StaticArrayTypes.StaticArrayNameList dResult) && Enum.IsDefined(typeof(Globals.StaticArrayTypes.StaticArrayNameList), dResult)) return true;
            else return false;
        }

        public static Type GetArrayVariableType<StructName>(string name) where StructName : struct
        {
            Type someType = typeof(StructName);

            Type? nestedType = someType.GetNestedType(name);
            if (nestedType != null)
            {
                return nestedType;
            }

            FieldInfo? fieldInfo = someType.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public); // Include public fields!
            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }

            throw new ArgumentException($"Type or Field {name} not found in type {typeof(StructName)}!");
        }

        public static void Instantiate(Type arrayReflectionType, UnrealArchive Ar, string arrayName, int arraySize, JsonParser dataParser, UPropertyManager.ArrayManagerState state) =>
            Activator.CreateInstance(arrayReflectionType, Ar, arrayName, arraySize, dataParser, state);  // create instance of class with generic typ

        public static Type CreateGenericType(Type genericType) =>
            typeof(UPropertyManager.UPropertyArrayManager<>).MakeGenericType(genericType);  

        public static void CreateArrayManager(UnrealArchive Ar, string arrayName, int arraySize, JsonParser dataParser, UPropertyManager.ArrayManagerState state)
        {
            Type? genericType;
            switch (state)
            {
                case UPropertyManager.ArrayManagerState.Static:
                    genericType = GetArrayVariableType<Globals.StaticArrayTypes>(arrayName);
                    break;
                case UPropertyManager.ArrayManagerState.Dynamic:
                    genericType = GetArrayVariableType<Globals.DynamicArrayTypes>(arrayName);
                    break;
                default:
                    genericType = default;
                    break;
            }
            // putting a band-aid on a dam
            Instantiate(CreateGenericType(genericType!), Ar, arrayName, arraySize, dataParser, state);
        }
    }