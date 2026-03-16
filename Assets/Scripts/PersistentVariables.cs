using UnityEngine;

[System.Serializable]
public class PersistentVariables
{
    public string defaultFileLocation;

    public PersistentVariables(string fileLoc)
    {
        defaultFileLocation = fileLoc;
    }
    public PersistentVariables(PersistentVariables vars)
    {
        defaultFileLocation = vars.defaultFileLocation;
    }
    public PersistentVariables()
    {
        defaultFileLocation = "C:\\Users";
    }

}
