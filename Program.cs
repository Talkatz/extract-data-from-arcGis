using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;

namespace spatial_analysis_cholera
{
    class Program
    {
        static void Main(string[] args)
        {
			// our dataTable will have the following structure
			DataTable pumpsDt = new DataTable();
			pumpsDt.Columns.Add("pumpId", typeof(int));
			pumpsDt.Columns.Add("x", typeof(double));
			pumpsDt.Columns.Add("y", typeof(double));
			pumpsDt.Columns.Add("avgDistToCholeraCases", typeof(double));
			
			// loop over the pumps dataTable and add the average distance from the cholera main cases
			for (int i = 0; i < pumpsDt.Rows.Count; i++)
			{
				double x = double.Parse(pumpsDt.Rows[i]["x"].ToString());
				double y = double.Parse(pumpsDt.Rows[i]["y"].ToString());
				pumpsDt.Rows[i]["avgDistToCholeraCases"] = distance_from_pump(x, y, <myArcmapFeatureLayer>);
			}

			// creating a new sorted table, in which the pumps on top will be those the smallest distance average, and that are needed accordingly to be checked first
			DataView dv = pumpsDt.DefaultView;
			dv.Sort = "avgDistToCholeraCases ASC";
			DataTable sortedPumpsDt = dv.ToTable();

			// creating a table that will hold the data from ArcMap
			DataTable buildings = getAddresses(<myArcmapFeatureLayer>);
		}

		//The methods 
		//The following method checks the distance for a point, polygon or polyline shape layer from a given point on ArcMap, and returns the average distance
		public static double distance_from_pump(double input_xCord, double input_yCord, IFeatureLayer2 FeatureLayer)
		{
			List<double> distList = new List<double>();
			double average_distance = (-1);

			try
			{
				ESRI.ArcGIS.Geometry.IPoint point = new ESRI.ArcGIS.Geometry.Point();  // creating a point from the input coordinates
				point.PutCoords(input_xCord, input_yCord);
				IGeometry g = point;

				IFeatureClass pFeatureClass = FeatureLayer.FeatureClass;
				IFeatureCursor pCursor = pFeatureClass.Search(null, false); //
				IFeature pFeat = pCursor.NextFeature(); //using a cursor to iterate the records of the given layer (the featureClass)
				while (pFeat != null)
				{
					double distToPump = 0;
					try
					{
						double residents = double.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("build_pop"))));
						double numOfCholeraCases = double.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("cholera_cases"))));
						if ((numOfCholeraCases / residents) >= 0.05) //if at least 5% of residents in the building are with cholera, check the distance 
						{
							distToPump = ((IProximityOperator)g).ReturnDistance(pFeat.Shape as IGeometry); // using ArcObjects ReturnDistance function
						}
						else
						{
							pFeat = pCursor.NextFeature();
							continue;
						}
					}
					catch (Exception ex)
					{
						// do something with the error, and continue with the other records

						pFeat = pCursor.NextFeature();
						continue;
					}

					//insert the current record to the list
					distList.Add(distToPump);

					pFeat = pCursor.NextFeature();
				}

				average_distance = distList.Average();
			}
			catch (Exception ex)
			{
				// do something with the error
				return average_distance;
			}
			return average_distance;
		}

		//The following method returns a dataTable that we populate with data from an ArcMap shape layer
		public static DataTable getAddresses(IFeatureLayer2 FeatureLayer)
		{
			// we create the dataTable and define the columns to match the data which we'll retrieve from the layer 
			DataTable outputDt = new DataTable();
			outputDt.Columns.Add("buildingID", typeof(int));
			outputDt.Columns.Add("address", typeof(string));
			outputDt.Columns.Add("x", typeof(double));
			outputDt.Columns.Add("y", typeof(double));
			outputDt.Columns.Add("buildingPop", typeof(int));
			outputDt.Columns.Add("choleraCases", typeof(int));
			outputDt.TableName = FeatureLayer.FeatureClass.AliasName; // if we want to give the dataTable the same name as the layer

			try
			{
				IFeatureClass pFeatureClass = FeatureLayer.FeatureClass;
				IFeatureCursor pCursor = pFeatureClass.Search(null, false); //
				IFeature pFeat = pCursor.NextFeature(); //using a cursor to iterate the records of the given layer (the featureClass)

				while (pFeat != null)
				{
					try
					{
						//inserting the current record into the dataTable
						DataRow row = outputDt.NewRow();
						row["buildingID"] = int.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("build_id"))));
						row["address"] = String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("address")));
						row["x"] = double.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("x"))));
						row["y"] = double.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("y"))));
						row["buildingPop"] = int.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("build_pop"))));
						row["choleraCases"] = int.Parse(String.Format("{0}", pFeat.get_Value(pFeat.Fields.FindField("cholera_cases"))));
						outputDt.Rows.Add(row);
					}
					catch (Exception ex)
					{
						// do something with the error
						pFeat = pCursor.NextFeature();
						continue;
					}

					pFeat = pCursor.NextFeature();
				}
			}
			catch (Exception ex)
			{
				// do something with the error
				return outputDt; // return empty table
			}
			return outputDt;
		}
	}
}
