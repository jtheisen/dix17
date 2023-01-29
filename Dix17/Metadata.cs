﻿using static Dix17.AdHocCreation;

namespace Dix17;


enum CoreType
{
    Ordered = 1,
    Unassociated = 2 // "list"
}

public static class Metadata
{
    public const String JsonType = "x:json-type";

    public const String JsonTypeObject = "object";
    public const String JsonTypeArray = "array";
    public const String JsonTypeBoolean = "boolean";
    public const String JsonTypeString = "string";
    public const String JsonTypeNumber = "number";
    public const String JsonTypeNull = "null";

    public const String ReflectedType = "reflection:type";

    public const String ReflectedTypeObject = "object";
    public const String ReflectedTypeEnumerable = "enumerable";
    public const String ReflectedTypeBoolean = "boolean";
    public const String ReflectedTypeString = "string";
    public const String ReflectedTypeNumber = "number";
    public const String ReflectedTypeNull = "null";
}
