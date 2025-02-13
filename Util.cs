using System;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace RiftVibeSolver;

public static class Util {
    private static readonly BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static Hook CreateMethodHook(this Type type, string name, Delegate method)
        => new(type.GetMethod(name, ALL_FLAGS), method);
}