using System.Text;
using STP.Lab2.Models;

namespace STP.Lab2;

public class Playlist
{
    public int Count { get; private set; }

    public int Capacity => Tracks.Length;
    private Track[] Tracks { get; set; }

    private HashSet<Track> UniqueTracks { get; set; }

    public Track this[int index]
    {
        get
        {
            if (index >= 0 && index < Count)
                return Tracks[index];

            throw new PlaylistException("Index out of range");
        }
        set
        {
            if (index < 0 || index >= Count)
                throw new PlaylistException("Index out of range");

            Tracks[index] = value;
            
            UniqueTracks.Clear();

            UniqueTracks = new HashSet<Track>(Tracks);
        }
    }

    public Playlist(int capacity)
    {
        if (capacity <= 0)
        {
            throw new PlaylistException("Playlist capacity must be positive");
        }

        Count = 0;
        Tracks = new Track[capacity];
        UniqueTracks = [];
    }

    public static bool operator ==(Playlist? left, Playlist? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;
        
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (left.Tracks[i] != right.Tracks[i])
                return false;
        }

        return true;
    }

    public static bool operator !=(Playlist? left, Playlist? right)
    {
        return !(left == right);
    }

    public void Add(Track track)
    {
        if (Count >= Capacity)
        {
            throw new PlaylistException("Playlist capacity exceeded");
        }

        if (!UniqueTracks.Add(track))
        {
            throw new PlaylistException("Track already added");
        }

        Tracks[Count] = track;
        Count++;
    }

    public void Merge(Playlist other)
    {
        var toAdd = new List<Track>();

        for (var i = 0; i < other.Count; i++)
        {
            var track = other.Tracks[i];

            if (UniqueTracks.Contains(track))
                toAdd.Add(track);
        }

        if (toAdd.Count > Capacity - Count)
            throw new PlaylistException("Not enough capacity");

        foreach (var track in toAdd)
        {
            Tracks[Count] = track;
            Count++;
            UniqueTracks.Add(track);
        }
    }

    public void RemoveTracksOf(Playlist other)
    {
        var write = 0;

        for (var i = 0; i < Count; i++)
        {
            if (other.UniqueTracks.Contains(Tracks[i]))
                continue;

            Tracks[write] = Tracks[i];
            write++;
        }

        if (write == Count)
            throw new PlaylistException("No matching tracks found");

        Array.Clear(Tracks, write, Count - write);
        Count = write;    
        
        UniqueTracks.ExceptWith(other.UniqueTracks);
    }

    public int TotalDuration()
    {
        var sum = 0;

        for (var i = 0; i < Count; i++)
            sum += Tracks[i].Duration;

        return sum;
    }

    public int FindByArtist(string artist)
    {
        for (var i = 0; i < Count; i++)
        {
            if (string.CompareOrdinal(Tracks[i].Artist, artist) == 0)
            {
                return i;
            }
        }

        throw new PlaylistException("No tracks found");
    }

    public override string ToString()
    {
        var res = new StringBuilder();
        for (var i = 0; i < Count; i++)
        {
            if (i > 0)
                res.AppendLine();

            res.Append(Tracks[i]);
        }

        return res.ToString();
    }
}