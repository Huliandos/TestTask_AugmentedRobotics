using UnityEngine;

public static class GeometryHelper
{
    // Given three collinear points p, q, r, the function checks if 
    // point q lies on line segment 'pr' 
    static bool OnSegment(Vector2 p, Vector2 q, Vector2 r) 
    { 
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && 
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y)) 
            return true; 
    
        return false; 
    } 
    
    // To find orientation of ordered triplet (p, q, r). 
    // The function returns following values 
    // 0 --> p, q and r are collinear 
    // 1 --> Clockwise 
    // 2 --> Counterclockwise 
    static int Orientation(Vector2 p, Vector2 q, Vector2 r) 
    { 
        // See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
        // for details of below formula. 
        float val = (q.y - p.y) * (r.x - q.x) - 
                (q.x - p.x) * (r.y - q.y); 
    
        if (val == 0) return 0; // collinear 
    
        return (val > 0)? 1: 2; // clock or counterclock wise 
    } 

    // The main function that returns true if line segment 'p1q1' 
    // and 'p2q2' intersect. 
    public static bool DoIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2) 
    { 
        // Find the four orientations needed for general and 
        // special cases 
        int o1 = Orientation(p1, q1, p2); 
        int o2 = Orientation(p1, q1, q2); 
        int o3 = Orientation(p2, q2, p1); 
        int o4 = Orientation(p2, q2, q1); 
    
        // General case 
        if (o1 != o2 && o3 != o4) 
            return true; 
    
        // Special Cases 
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1 
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true; 
    
        // p1, q1 and q2 are collinear and q2 lies on segment p1q1 
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true; 
    
        // p2, q2 and p1 are collinear and p1 lies on segment p2q2 
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true; 
    
        // p2, q2 and q1 are collinear and q1 lies on segment p2q2 
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true; 
    
        return false; // Doesn't fall in any of the above cases 
    } 

    
    // Function to check if a point is inside a polygon
    public static bool PointInPolygon(Vector2 point, Unity.Collections.NativeArray<Vector2> polygon)
    {
        int numVertices = polygon.Length;
        double x = point.x, y = point.y;
        bool inside = false;
 
        // Store the first point in the polygon and initialize the second point
        Vector2 p1 = polygon[0], p2;
 
        // Loop through each edge in the polygon
        for (int i = 1; i <= numVertices; i++)
        {
            // Get the next point in the polygon
            p2 = polygon[i % numVertices];
 
            // Check if the point is above the minimum y coordinate of the edge
            if (y > Mathf.Min(p1.y, p2.y))
            {
                // Check if the point is below the maximum y coordinate of the edge
                if (y <= Mathf.Max(p1.y, p2.y))
                {
                    // Check if the point is to the left of the maximum x coordinate of the edge
                    if (x <= Mathf.Max(p1.x, p2.x))
                    {
                        // Calculate the x-intersection of the line connecting the point to the edge
                        double xIntersection = (y - p1.y) * (p2.x - p1.x) / (p2.y - p1.y) + p1.x;
 
                        // Check if the point is on the same line as the edge or to the left of the x-intersection
                        if (p1.x == p2.x || x <= xIntersection)
                        {
                            // Flip the inside flag
                            inside = !inside;
                        }
                    }
                }
            }
 
            // Store the current point as the first point for the next iteration
            p1 = p2;
        }
 
        // Return the value of the inside flag
        return inside;
    }
}
