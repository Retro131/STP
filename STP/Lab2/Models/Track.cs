namespace STP.Lab2.Models;

public class Track(string title, string artist, int duration)
{
    public string Title { get; } = title;
    public string Artist { get; } = artist;
    public int Duration { get; } = duration;


    public static bool operator ==(Track? left, Track? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;
        
        return left.Title == right.Title && left.Artist == right.Artist;
    }

    public static bool operator !=(Track? left, Track? right)
    {
        return !(left == right);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Track other &&
               Title == other.Title &&
               Artist == other.Artist;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Title, Artist);
    }

    public override string ToString()
    {
        return $"{Artist} -- {Title} ({TimeSpan.FromMinutes(Duration):mm\\:ss})";
    }
}