using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fasico.DataAccess.Management.GIS;
using Fasico.General.DTO.Management.GIS;

namespace CalculateCenterShape
{
    class Program
    {
        static void Main(string[] args)
        {
            var dal = new MapDAL();
            var dt = dal.Select(new MapDTO());
            var list = new List<MapDTO>();
            foreach (DataRow dr in dt.Rows)
            {
                list.Add(new MapDTO(dr));
            }

            foreach (var dto in list)
            {
                if (!string.IsNullOrEmpty(dto.mapShapeText))
                {
                    dto.mapCenter = Calculate(dto.mapShapeText);
                }
            }
            using (var connection = new SqlConnection("Data Source=.;Initial Catalog=FasicoMang;Persist Security Info=True;User ID=sa;Password=123456"))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    if (connection.State == ConnectionState.Closed)
                        connection.Open();
                    foreach (var dto in list.Where(x => x.mapCenter != null))
                    {
                        command.CommandText = "UPDATE GIS.TBL_Map SET mapCenter =N'" + dto.mapCenter +
                                              "' WHERE id='" + dto.ID + "'";
                        command.ExecuteNonQuery();
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
        }

        private static string Calculate(string shapeText)
        {
            var centerReturn = string.Empty;
            var points = new List<PointF>();
            var r = new Regex("^[A-z]+");
            var temp = r.Replace(shapeText, "");
            temp = temp.Replace("(", "");
            temp = temp.Replace(")", "");
            var stringPoints = temp.Split(',');
            foreach (var point in stringPoints)
            {
                if (point.StartsWith(" "))
                {
                    var trimedPoint = point.TrimStart(' ');
                    var longLatString = trimedPoint.Split(' ');
                    points.Add(new PointF(float.Parse(longLatString[0].ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                        float.Parse(longLatString[1].ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)));
                }
                else
                {
                    var longLatString = point.Split(' ');
                    points.Add(new PointF(float.Parse(longLatString[0].ToString(CultureInfo.InvariantCulture)),
                        float.Parse(longLatString[1].ToString(CultureInfo.InvariantCulture))));
                }
            }
            if (points.Any())
            {
                var center = FindCentroid(points.ToArray());
                centerReturn = "Point (" + center.X.ToString(CultureInfo.InvariantCulture) + " " + center.Y.ToString(CultureInfo.InvariantCulture) + ")";
            }
            return centerReturn;
        }
        private static PointF FindCentroid(PointF[] points)
        {
            // Add the first point at the end of the array.
            var numPoints = points.Length;
            var pts = new PointF[numPoints + 1];
            points.CopyTo(pts, 0);
            pts[numPoints] = points[0];

            // Find the centroid.
            float x = 0;
            float y = 0;
            for (var i = 0; i < numPoints; i++)
            {
                var secondFactor = pts[i].X * pts[i + 1].Y -
                                      pts[i + 1].X * pts[i].Y;
                x += (pts[i].X + pts[i + 1].X) * secondFactor;
                y += (pts[i].Y + pts[i + 1].Y) * secondFactor;
            }

            // Divide by 6 times the polygon's area.
            var polygonArea = PolygonArea(points);
            x /= (6 * polygonArea);
            y /= (6 * polygonArea);

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (x < 0)
            {
                x = -x;
                y = -y;
            }

            return new PointF(x, y);
        }

        private static float PolygonArea(PointF[] points)
        {
            // Return the absolute value of the signed area.
            // The signed area is negative if the polygon is
            // oriented clockwise.
            return Math.Abs(SignedPolygonArea(points));
        }

        private static float SignedPolygonArea(PointF[] points)
        {
            // Add the first point to the end.
            var numPoints = points.Length;
            var pts = new PointF[numPoints + 1];
            points.CopyTo(pts, 0);
            pts[numPoints] = points[0];

            // Get the areas.
            float area = 0;
            for (var i = 0; i < numPoints; i++)
            {
                area +=
                    (pts[i + 1].X - pts[i].X) *
                    (pts[i + 1].Y + pts[i].Y) / 2;
            }

            // Return the result.
            return area;
        }
    }
}
