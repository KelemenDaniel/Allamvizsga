using System;
using UnityEngine;

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }
}
