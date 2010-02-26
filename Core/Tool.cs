/******************************************************************************
  Copyright 2009 dataweb GmbH
  This file is part of the NShape framework.
  NShape is free software: you can redistribute it and/or modify it under the 
  terms of the GNU General Public License as published by the Free Software 
  Foundation, either version 3 of the License, or (at your option) any later 
  version.
  NShape is distributed in the hope that it will be useful, but WITHOUT ANY
  WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR 
  A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
  You should have received a copy of the GNU General Public License along with 
  NShape. If not, see <http://www.gnu.org/licenses/>.
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Timers;

using Dataweb.NShape.Controllers;


namespace Dataweb.NShape.Advanced {

	public class CursorProvider {

		/// <summary>
		/// Registers a custom cursor that can be used with SetCursor.
		/// </summary>
		/// <param name="fileName">The file name of the cursor resource.</param>
		/// <returns>Id of the cursor.</returns>
		public static int RegisterCursor(string fileName) {
			if (fileName == null) throw new ArgumentNullException("fileName");
			byte[] resource = null;
			FileStream stream = new FileStream(fileName, FileMode.Open);
			try {
				resource = new byte[stream.Length];
				stream.Read(resource, 0, resource.Length);
			} finally {
				stream.Close();
				stream.Dispose();
			}
			return RegisterCursorResource(resource);
		}


		/// <summary>
		/// Registers a custom cursor that can be used with SetCursor.
		/// </summary>
		/// <param name="resourceAssembly">Assembly containing the cursor resource.</param>
		/// <param name="resourceName">The name of the cursor resource.</param>
		/// <returns>Id of the cursor.</returns>
		public static int RegisterCursor(Assembly resourceAssembly, string resourceName) {
			if (resourceAssembly == null) throw new ArgumentNullException("resourceAssembly");
			if (resourceName == null) throw new ArgumentNullException("resourceName");
			byte[] resource = null;
			Stream stream = resourceAssembly.GetManifestResourceStream(resourceName);
			try {
				resource = new byte[stream.Length];
				stream.Read(resource, 0, resource.Length);
			} finally {
				stream.Close();
				stream.Dispose();
			}
			return RegisterCursorResource(resource);
		}


		/// <summary>
		/// Registers a custom cursor that can be used with SetCursor.
		/// </summary>
		/// <param name="fileName">The cursor resource.</param>
		/// <returns>Id of the cursor.</returns>
		public static int RegisterCursor(byte[] resource) {
			if (resource == null) throw new ArgumentNullException("resource");
			return RegisterCursorResource(resource);
		}


		/// <summary>
		/// Returns all registered cursors.
		/// CursorId 0 means the system's default cursor which is not stored as resource.
		/// </summary>
		public static IEnumerable<int> CursorIDs {
			get { return registeredCursors.Keys; }
		}


		/// <summary>
		/// Returns the resource associated with the given cursorID. 
		/// CursorId 0 means the system's default cursor which is not stored as resource.
		/// </summary>
		/// <param name="cursorID">ID of the cursor returned by the RegisterCursor method.</param>
		/// <returns></returns>
		public static byte[] GetResource(int cursorID) {
			if (cursorID == DefaultCursorID) return null;
			return registeredCursors[cursorID];
		}


		public const int DefaultCursorID = 0;


		private static int RegisterCursorResource(byte[] resource) {
			// Check if the resource was registered
			foreach (KeyValuePair<int, byte[]> item in registeredCursors) {
				if (item.Value.Length == resource.Length) {
					bool equal = true;
					for (int i = item.Value.Length - 1; i >= 0; --i) {
						if (item.Value[i] != resource[i]) {
							equal = false;
							break;
						}
					}
					if (equal) return item.Key;
				}
			}
			// Register resource
			int cursorId = registeredCursors.Count + 1;
			registeredCursors.Add(cursorId, resource);
			return cursorId;
		}


		private static Dictionary<int, byte[]> registeredCursors = new Dictionary<int, byte[]>();
	}


	/// <summary>
	/// Specifies the outcome of a tool execution.
	/// </summary>
	/// <status>reviewed</status>
	public enum ToolResult {
		/// <summary>Tool was successfully executed</summary>
		Executed,
		/// <summary>Tool was canceled</summary>
		Canceled
	}


	/// <summary>
	/// Describes how a tool was executed.
	/// </summary>
	/// <status>reviewed</status>
	public class ToolExecutedEventArgs : EventArgs {

		public ToolExecutedEventArgs(Tool tool, ToolResult eventType)
			: base() {
			if (tool == null) throw new ArgumentNullException("tool");
			this.tool = tool;
			this.eventType = eventType;
		}


		public Tool Tool {
			get { return tool; }
		}


		public ToolResult EventType {
			get { return eventType; }
		}


		private Tool tool;
		private ToolResult eventType;

	}


	/// <summary>
	/// Controls a user operation on a diagram.
	/// </summary>
	/// <status>reviewed</status>
	public abstract class Tool : IDisposable {

		#region IDisposable Members

		public virtual void Dispose() {
			if (smallIcon != null)
				smallIcon.Dispose();
			smallIcon = null;

			if (largeIcon != null)
				largeIcon.Dispose();
			largeIcon = null;
		}

		#endregion


		public string Name {
			get { return name; }
		}


		public string Title {
			get { return title; }
			set { title = value; }
		}


		public virtual string Description {
			// TODO 2: Remove this implementation, when all derived classes have a better one.
			get { return description; }
			set { description = value; }
		}


		public string Category {
			get { return category; }
			set { category = value; }
		}


		public string ToolTipText {
			get { return Description; }
			set { Description = value; }
		}


		public Bitmap SmallIcon {
			get { return smallIcon; }
			set { smallIcon = value; }
		}


		public Bitmap LargeIcon {
			get { return largeIcon; }
			set { largeIcon = value; }
		}


		public abstract void EnterDisplay(IDiagramPresenter diagramPresenter);


		public abstract void LeaveDisplay(IDiagramPresenter diagramPresenter);


		/// <summary>
		/// Processes a mouse event.
		/// The base Method has to be called at the end when overriding this implementation.
		/// </summary>
		/// <param name="display">Diagram presenter where the event occurred.</param>
		/// <param name="e">Description of the mouse event.</param>
		/// <returns>True if the event was handled, false if the event was not handled.</returns>
		public virtual bool ProcessMouseEvent(IDiagramPresenter diagramPresenter, MouseEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("display");
			currentMouseState.Buttons = e.Buttons;
			currentMouseState.Modifiers = e.Modifiers;
			diagramPresenter.ControlToDiagram(e.Position, out currentMouseState.Position);
			return false;
		}


		/// <summary>
		/// Processes a keyboard event.
		/// </summary>
		/// <param name="diagramPresenter">Diagram presenter where the event occurred.</param>
		/// <param name="e">Description of the keyboard event.</param>
		/// <returns>True if the event was handled, false if the event was not handled.</returns>
		public virtual bool ProcessKeyEvent(IDiagramPresenter diagramPresenter, KeyEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			bool result = false;
			switch (e.EventType) {
				case KeyEventType.KeyDown:
					// Cancel tool
					if (e.KeyCode == (int)KeysDg.Escape) {
						Cancel();
						result = true;
					}
					break;

				case KeyEventType.KeyPress:
				case KeyEventType.PreviewKeyDown:
				case KeyEventType.KeyUp:
					// do nothing
					break;
				default: throw new NShapeUnsupportedValueException(e.EventType);
			}
			return result;
		}


		/// <summary>
		/// Sets protected readonly-properties to invalid values and raises the ToolExecuted event.
		/// </summary>
		public void Cancel() {
			// End the tool's action
			while (IsToolActionPending)
				EndToolAction();

			// Reset the tool's state
			CancelCore();

			currentMouseState = MouseState.Empty;

			OnToolExecuted(CancelledEventArgs);
		}


		/// <summary>
		/// Specifis if the tool wants the diagram presenter to scroll when reaching the presenter's bounds.
		/// </summary>
		public virtual bool WantsAutoScroll {
			get {
				if (pendingActions.Count == 0) return false;
				else return pendingActions.Peek().WantsAutoScroll;
			}
		}


		public abstract IEnumerable<MenuItemDef> GetMenuItemDefs(IDiagramPresenter diagramPresenter);


		public abstract void Invalidate(IDiagramPresenter diagramPresenter);


		public abstract void Draw(IDiagramPresenter diagramPresenter);


		public abstract void RefreshIcons();


		/// <summary>
		/// Occurs when the tool was executed or canceled.
		/// </summary>
		public event EventHandler<ToolExecutedEventArgs> ToolExecuted;


		protected Tool() {
			smallIcon = new Bitmap(16, 16);
			largeIcon = new Bitmap(32, 32);
			name = "Tool " + this.GetHashCode().ToString();
			ExecutedEventArgs = new ToolExecutedEventArgs(this, ToolResult.Executed);
			CancelledEventArgs = new ToolExecutedEventArgs(this, ToolResult.Canceled);
		}


		protected Tool(string category)
			: this() {
			if (!string.IsNullOrEmpty(category))
				this.category = category;
		}


		~Tool() {
			Dispose();
		}


		protected abstract void CancelCore();


		protected virtual void OnToolExecuted(ToolExecutedEventArgs eventArgs) {
			if (IsToolActionPending) throw new InvalidOperationException(string.Format("{0} tool actions pending.", pendingActions.Count));
			if (ToolExecuted != null) ToolExecuted(this, eventArgs);
		}


		/// <summary>
		/// Finds the nearest snap point for a point.
		/// </summary>
		/// <param name="ptX">X coordinate</param>
		/// <param name="ptY">Y coordinate</param>
		/// <param name="snapDeltaX">Horizontal distance between x and the nearest snap point.</param>
		/// <param name="snapDeltaY">Vertical distance between y and the nearest snap point.</param>
		/// <returns>Distance to nearest snap point.</returns>
		/// <remarks>If snapping is disabled for the current ownerDisplay, this function does virtually nothing.</remarks>
		protected float FindNearestSnapPoint(IDiagramPresenter diagramPresenter, int x, int y, out int snapDeltaX, out int snapDeltaY) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");

			float distance = float.MaxValue;
			snapDeltaX = snapDeltaY = 0;
			if (diagramPresenter.SnapToGrid) {
				// calculate position of surrounding grid lines
				int gridSize = diagramPresenter.GridSize;
				int left = x - (x % gridSize);
				int above = y - (y % gridSize);
				int right = x - (x % gridSize) + gridSize;
				int below = y - (y % gridSize) + gridSize;
				float currDistance = 0;
				int snapDistance = diagramPresenter.SnapDistance;

				// calculate distance from the given point to the surrounding grid lines
				currDistance = y - above;
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaY = above - y;
				}
				currDistance = right - x;
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = right - x;
				}
				currDistance = below - y;
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaY = below - y;
				}
				currDistance = x - left;
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = left - x;
				}

				// calculate approximate distance from the given point to the surrounding grid points
				currDistance = Geometry.DistancePointPoint(x, y, left, above);
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = left - x;
					snapDeltaY = above - y;
				}
				currDistance = Geometry.DistancePointPoint(x, y, right, above);
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = right - x;
					snapDeltaY = above - y;
				}
				currDistance = Geometry.DistancePointPoint(x, y, left, below);
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = left - x;
					snapDeltaY = below - y;
				}
				currDistance = Geometry.DistancePointPoint(x, y, right, below);
				if (currDistance <= snapDistance && currDistance >= 0 && currDistance < distance) {
					distance = currDistance;
					snapDeltaX = right - x;
					snapDeltaY = below - y;
				}
			}
			return distance;
		}


		/// <summary>
		/// Finds the nearest SnapPoint in range of the given shape's control point.
		/// </summary>
		/// <param name="shape">The shape for which the nearest snap point is searched.</param>
		/// <param name="connectionPointId">The control point of the shape.</param>
		/// <param name="moveByX">Declares the distance, the shape is moved on X axis before finding snap point.</param>
		/// <param name="moveByY">Declares the distance, the shape is moved on X axis before finding snap point.</param>
		/// <param name="snapDeltaX">Horizontal distance between ptX and the nearest snap point.</param>
		/// <param name="snapDeltaY">Vertical distance between ptY and the nearest snap point.</param>
		/// <returns>Distance to nearest snap point.</returns>
		protected float FindNearestSnapPoint(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId controlPointId,
			int pointOffsetX, int pointOffsetY, out int snapDeltaX, out int snapDeltaY) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (shape == null) throw new ArgumentNullException("shape");

			snapDeltaX = snapDeltaY = 0;
			Point p = shape.GetControlPointPosition(controlPointId);
			return FindNearestSnapPoint(diagramPresenter, p.X + pointOffsetX, p.Y + pointOffsetY, out snapDeltaX, out snapDeltaY);
		}


		/// <summary>
		/// Finds the nearest SnapPoint in range of the given shape.
		/// </summary>
		/// <param name="shape">The shape for which the nearest snap point is searched.</param>
		/// <param name="shapeOffsetX">Declares the distance, the shape is moved on X axis 
		/// before finding snap point.</param>
		/// <param name="shapeOffsetY">Declares the distance, the shape is moved on X axis 
		/// before finding snap point.</param>
		/// <param name="snapDeltaX">Horizontal distance between ptX and the nearest snap point.</param>
		/// <param name="snapDeltaY">Vertical distance between ptY and the nearest snap point.</param>
		/// <returns>Distance to the calculated snap point.</returns>
		protected float FindNearestSnapPoint(IDiagramPresenter diagramPresenter, Shape shape, int shapeOffsetX, int shapeOffsetY,
			out int snapDeltaX, out int snapDeltaY) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (shape == null) throw new ArgumentNullException("shape");

			snapDeltaX = snapDeltaY = 0;
			int snapDistance = diagramPresenter.SnapDistance;
			float lowestDistance = float.MaxValue;

			Rectangle shapeBounds = shape.GetBoundingRectangle(true);
			shapeBounds.Offset(shapeOffsetX, shapeOffsetY);
			int boundsCenterX = (int)Math.Round(shapeBounds.X + shapeBounds.Width / 2f);
			int boundsCenterY = (int)Math.Round(shapeBounds.Y + shapeBounds.Width / 2f);

			int dx, dy;
			float currDistance;
			// Calculate snap distance of center point
			currDistance = FindNearestSnapPoint(diagramPresenter, boundsCenterX, boundsCenterY, out dx, out dy);
			if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
				lowestDistance = currDistance;
				snapDeltaX = dx;
				snapDeltaY = dy;
			}

			// Calculate snap distance of bounding rectangle
			currDistance = FindNearestSnapPoint(diagramPresenter, shapeBounds.Left, shapeBounds.Top, out dx, out dy);
			if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
				lowestDistance = currDistance;
				snapDeltaX = dx;
				snapDeltaY = dy;
			}
			currDistance = FindNearestSnapPoint(diagramPresenter, shapeBounds.Right, shapeBounds.Top, out dx, out dy);
			if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
				lowestDistance = currDistance;
				snapDeltaX = dx;
				snapDeltaY = dy;
			}
			currDistance = FindNearestSnapPoint(diagramPresenter, shapeBounds.Left, shapeBounds.Bottom, out dx, out dy);
			if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
				lowestDistance = currDistance;
				snapDeltaX = dx;
				snapDeltaY = dy;
			}
			currDistance = FindNearestSnapPoint(diagramPresenter, shapeBounds.Right, shapeBounds.Bottom, out dx, out dy);
			if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
				lowestDistance = currDistance;
				snapDeltaX = dx;
				snapDeltaY = dy;
			}
			return lowestDistance;
		}


		/// <summary>
		/// Finds the nearest SnapPoint in range of the given shape.
		/// </summary>
		/// <param name="shape">The shape for which the nearest snap point is searched.</param>
		/// <param name="moveByX">Declares the distance, the shape is moved on X axis 
		/// before finding snap point.</param>
		/// <param name="moveByY">Declares the distance, the shape is moved on X axis 
		/// before finding snap point.</param>
		/// <param name="snapDeltaX">Horizontal distance between ptX and the nearest snap point.</param>
		/// <param name="snapDeltaY">Vertical distance between ptY and the nearest snap point.</param>
		/// <param name="controlPointCapabilities">Filter for control points taken into 
		/// account while calculating the snap distance.</param>
		/// <returns>Control point of the shape, the calculated distance refers to.</returns>
		protected ControlPointId FindNearestSnapPoint(IDiagramPresenter diagramPresenter, Shape shape, int pointOffsetX, int pointOffsetY,
			out int snapDeltaX, out int snapDeltaY, ControlPointCapabilities controlPointCapability) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (shape == null) throw new ArgumentNullException("shape");

			snapDeltaX = snapDeltaY = 0;
			ControlPointId result = ControlPointId.None;
			int snapDistance = diagramPresenter.SnapDistance;
			float lowestDistance = float.MaxValue;
			foreach (ControlPointId ptId in shape.GetControlPointIds(controlPointCapability)) {
				int dx, dy;
				float currDistance = FindNearestSnapPoint(diagramPresenter, shape, ptId, pointOffsetX, pointOffsetY, out dx, out dy);
				if (currDistance < lowestDistance && currDistance >= 0 && currDistance <= snapDistance) {
					lowestDistance = currDistance;
					result = ptId;
					snapDeltaX = dx;
					snapDeltaY = dy;
				}
			}
			return result;
		}


		/// <summary>
		/// Finds the nearest ControlPoint in range of the given shape's ControlPoint. 
		/// If there is no ControlPoint in range, the snap distance to the nearest grid 
		/// line will be calculated.
		/// </summary>
		/// <param name="shape">The given shape.</param>
		/// <param name="connectionPointId">the given shape's ControlPoint</param>
		/// <param name="moveByX">Declares the distance, the shape is moved on X axis before finding snap point.</param>
		/// <param name="moveByY">Declares the distance, the shape is moved on X axis before finding snap point.</param>
		/// <param name="snapDeltaX">Horizontal distance between ptX and the nearest snap point.</param>
		/// <param name="snapDeltaY">Vertical distance between ptY and the nearest snap point.</param>
		/// <param name="ownPointId">The Id of the returned shape's nearest ControlPoint.</param>
		/// <returns>The shape owning the nearest ControlPoint</returns>
		protected Shape FindNearestControlPoint(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId controlPointId,
			ControlPointCapabilities targetPointCapabilities, int pointOffsetX, int pointOffsetY,
			out int snapDeltaX, out int snapDeltaY, out ControlPointId resultPointId) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (shape == null) throw new ArgumentNullException("shape");

			Shape result = null;
			snapDeltaX = snapDeltaY = 0;
			resultPointId = ControlPointId.None;

			if (diagramPresenter.Diagram != null) {
				// calculate new position of the ControlPoint
				Point ctrlPtPos = shape.GetControlPointPosition(controlPointId);
				ctrlPtPos.Offset(pointOffsetX, pointOffsetY);

				int snapDistance = diagramPresenter.SnapDistance;
				int resultZOrder = int.MinValue;
				IEnumerable<Shape> foundShapes = diagramPresenter.Diagram.Shapes.FindShapes(
						ctrlPtPos.X, ctrlPtPos.Y, ControlPointCapabilities.Connect, snapDistance);
				foreach (Shape foundShape in foundShapes) {
					if (foundShape == shape) continue;
					// Find the nearest control point
					float distance, lowestDistance = float.MaxValue;
					ControlPointId foundPtId = foundShape.FindNearestControlPoint(
							ctrlPtPos.X, ctrlPtPos.Y, snapDistance, targetPointCapabilities);
					//
					// Skip shapes without matching control points or below the last matching shape
					if (foundPtId == ControlPointId.None) continue;
					if (foundShape.ZOrder < resultZOrder) continue;
					//
					// If a valid control point was found, check wether it matches the criteria
					if (foundPtId == ControlPointId.Reference) {
						// If the shape itself is hit, do not calculate the snap distance because snapping 
						// to "real" control point has a higher priority.
						// Set TargetPointId and result shape in order to skip snapping to gridlines
						resultZOrder = foundShape.ZOrder;
						resultPointId = foundPtId;
						result = foundShape;
					} else {
						Point targetPtPos = foundShape.GetControlPointPosition(foundPtId);
						distance = Geometry.DistancePointPoint(ctrlPtPos.X, ctrlPtPos.Y, targetPtPos.X, targetPtPos.Y);
						if (distance <= snapDistance && distance < lowestDistance) {
							lowestDistance = distance;
							snapDeltaX = targetPtPos.X - ctrlPtPos.X;
							snapDeltaY = targetPtPos.Y - ctrlPtPos.Y;

							resultZOrder = foundShape.ZOrder;
							resultPointId = foundPtId;
							result = foundShape;
						}
					}
				}
				// calcualte distance to nearest grid point if there is no suitable control point in range
				if (resultPointId == ControlPointId.None)
					FindNearestSnapPoint(diagramPresenter, ctrlPtPos.X, ctrlPtPos.Y, out snapDeltaX, out snapDeltaY);
			}
			return result;
		}


		/// <summary>
		/// Find the topmost shape that is not selected and has a valid ConnectionPoint (or ReferencePoint) 
		/// in range of the given point.
		/// </summary>
		protected ShapeAtCursorInfo FindConnectionTarget(IDiagramPresenter diagramPresenter, int x, int y, bool onlyUnselected) {
		   return DoFindConnectionTarget(diagramPresenter, null, ControlPointId.None, x, y, onlyUnselected);
		}


		/// <summary>
		/// Find the topmost shape that is not selected and has a valid ConnectionPoint (or ReferencePoint) 
		/// in range of the given point.
		/// </summary>
		protected ShapeAtCursorInfo FindConnectionTarget(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId gluePointId, Point newGluePointPos, bool onlyUnselected) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (shape == null) throw new ArgumentNullException("shape");
			// Find (non-selected shape) its connection point under cursor
			ShapeAtCursorInfo result = ShapeAtCursorInfo.Empty;
			if (diagramPresenter.Diagram != null)
				result = DoFindConnectionTarget(diagramPresenter, shape, gluePointId, newGluePointPos.X, newGluePointPos.Y, onlyUnselected);
			return result;
		}


		/// <summary>
		/// Find the topmost shape that is at the given point or has a control point with the given
		/// capabilities in range of the given point. If parameter onlyUnselected is true, only 
		/// shapes that are not selected will be returned.
		/// </summary>
		protected ShapeAtCursorInfo FindShapeAtCursor(IDiagramPresenter diagramPresenter, int x, int y, ControlPointCapabilities capabilities, int range, bool onlyUnselected) {
			// Find non-selected shape its connection point under cursor
			ShapeAtCursorInfo result = ShapeAtCursorInfo.Empty;
			int zOrder = int.MinValue;
			foreach (Shape shape in diagramPresenter.Diagram.Shapes.FindShapes(x, y, capabilities, range)) {
				// Skip selected shapes (if not wanted)
				if (onlyUnselected && diagramPresenter.SelectedShapes.Contains(shape)) continue;

				// No need to handle Parent shapes here as Children of CompositeShapes cannot be 
				// selected and grouped shapes keep their ZOrder

				// Skip shapes below the last matching shape
				if (shape.ZOrder < zOrder) continue;
				zOrder = shape.ZOrder;
				result.Shape = shape;
				result.ControlPointId = shape.HitTest(x, y, capabilities, range);
				if (result.Shape is ICaptionedShape)
					result.CaptionIndex = ((ICaptionedShape)shape).FindCaptionFromPoint(x, y);
			}
			return result;
		}


		protected void InvalidateConnectionTargets(IDiagramPresenter diagramPresenter, int currentPosX, int currentPosY) {
			// invalidate selectedShapes in last range
			diagramPresenter.InvalidateGrips(shapesInRange, ControlPointCapabilities.Connect);

			if (Geometry.IsValid(currentPosX, currentPosY)) {
				ShapeAtCursorInfo shapeAtCursor = DoFindConnectionTarget(diagramPresenter, currentPosX, currentPosY, false);
				if (!shapeAtCursor.IsEmpty) shapeAtCursor.Shape.Invalidate();

				// invalidate selectedShapes in current range
				shapesInRange.Clear();
				shapesInRange.AddRange(diagramPresenter.Diagram.Shapes.FindShapes(currentPosX, currentPosY, ControlPointCapabilities.Connect, pointHighlightRange));
				if (shapesInRange.Count > 0)
					diagramPresenter.InvalidateGrips(shapesInRange, ControlPointCapabilities.Connect);
			}
		}


		protected void DrawConnectionTargets(IDiagramPresenter diagramPresenter, int x, int y) {
			Point p = Point.Empty;
			p.Offset(x, y);
			DrawConnectionTargets(diagramPresenter, null, ControlPointId.None, p, EmptyEnumerator<Shape>.Empty);
		}


		protected void DrawConnectionTargets(IDiagramPresenter diagramPresenter, int x, int y, IEnumerable<Shape> excludedShapes) {
			Point p = Point.Empty;
			p.Offset(x, y);
			DrawConnectionTargets(diagramPresenter, null, ControlPointId.None, p, excludedShapes);
		}


		protected void DrawConnectionTargets(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId gluePtId, Point newGluePtPos) {
			DrawConnectionTargets(diagramPresenter, shape, gluePtId, newGluePtPos, EmptyEnumerator<Shape>.Empty);
		}


		protected void DrawConnectionTargets(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId gluePtId, Point newGluePtPos, IEnumerable<Shape> excludedShapes) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			//if (shape == null) throw new ArgumentNullException("shape");
			//if (gluePtId == ControlPointId.None || gluePtId == ControlPointId.Any)
			//   throw new ArgumentException(string.Format("{0} is not a valid {1} for this operation.", gluePtId, typeof(ControlPointId).Name));
			//if (!shape.HasControlPointCapability(gluePtId, ControlPointCapabilities.Glue))
			//   throw new ArgumentException(string.Format("{0} is not a valid glue point.", gluePtId));
			if (diagramPresenter.Project.SecurityManager.IsGranted(Permission.Connect)) {
				// Find connection target shape at the given position
				ShapeAtCursorInfo shapeAtCursor = ShapeAtCursorInfo.Empty;
				if (shape != null && gluePtId != ControlPointId.None)
					shapeAtCursor = FindConnectionTarget(diagramPresenter, shape, gluePtId, newGluePtPos, false);
				else shapeAtCursor = FindConnectionTarget(diagramPresenter, newGluePtPos.X, newGluePtPos.Y, false);

				// Add shapes in range to the shapebuffer and then remove all excluded shapes
				shapeBuffer.Clear();
				shapeBuffer.AddRange(shapesInRange);
				foreach (Shape excludedShape in excludedShapes) {
					shapeBuffer.Remove(excludedShape);
					if (excludedShape == shapeAtCursor.Shape)
						shapeAtCursor.Clear();
				}

				// If there is no ControlPoint under the Cursor and the cursor is over a shape, draw the shape's outline
				if (!shapeAtCursor.IsEmpty && shapeAtCursor.ControlPointId == ControlPointId.Reference
					&& shapeAtCursor.Shape.ContainsPoint(newGluePtPos.X, newGluePtPos.Y)) {
					diagramPresenter.DrawShapeOutline(IndicatorDrawMode.Highlighted, shapeAtCursor.Shape);
				}

				// Draw all connectionPoints of all shapes in range (except the excluded ones, see above)
				diagramPresenter.ResetTransformation();
				try {
					for (int i = shapeBuffer.Count - 1; i >= 0; --i) {
						foreach (int ptId in shapeBuffer[i].GetControlPointIds(ControlPointCapabilities.Connect)) {
							IndicatorDrawMode drawMode = IndicatorDrawMode.Normal;
							if (shapeBuffer[i] == shapeAtCursor.Shape && ptId == shapeAtCursor.ControlPointId)
								drawMode = IndicatorDrawMode.Highlighted;
							diagramPresenter.DrawConnectionPoint(drawMode, shapeBuffer[i], ptId);
						}
					}
				} finally { diagramPresenter.RestoreTransformation(); }
			}
		}


		/// <summary>
		/// Sets the start coordinates for an action as well as the display to use for the action.
		/// </summary>
		protected virtual void StartToolAction(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantAutoScroll) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (mouseState == MouseState.Empty) throw new ArgumentException("mouseState");
			if (pendingActions.Count > 0) {
				if (pendingActions.Peek().DiagramPresenter != diagramPresenter)
					throw new NShapeException("There are actions pending for an other diagram presenter!");
			}
			ActionDef actionDef = ActionDef.Create(diagramPresenter, action, mouseState, wantAutoScroll);
			pendingActions.Push(actionDef);
		}


		[Obsolete]
		public bool ToolActionPending {
			get { return IsToolActionPending; }
		}


		/// <summary>
		/// Indicates if the tool has pending actions.
		/// </summary>
		public bool IsToolActionPending {
			get { return pendingActions.Count > 0; }
		}


		internal void Assert(bool condition) {
#if DEBUG
			Assert(condition, null);
#endif
		}


		internal void Assert(bool condition, string message) {
			if (condition == false) {
				if (string.IsNullOrEmpty(message)) throw new NShapeInternalException("Assertion Failure.");
				else throw new NShapeInternalException(string.Format("Assertion Failure: {0}", message));
			}
		}


		/// <summary>
		/// Ends a tool's action. Crears the start position for the action and the display used for the action.
		/// </summary>
		protected virtual void EndToolAction() {
			if (pendingActions.Count <= 0) throw new InvalidOperationException("No tool actions pending.");
			IDiagramPresenter diagramPresenter = pendingActions.Peek().DiagramPresenter;
			if (diagramPresenter != null) {
				Invalidate(diagramPresenter);
				diagramPresenter.Capture = false;
				diagramPresenter.SetCursor(CursorProvider.DefaultCursorID);
			}
			pendingActions.Pop();
		}


		protected bool IsGripHit(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId controlPointId, int x, int y) {
			if (shape == null) throw new ArgumentNullException("shape");
			Point p = shape.GetControlPointPosition(controlPointId);
			return IsGripHit(diagramPresenter, p.X, p.Y, x, y);
		}


		protected bool IsGripHit(IDiagramPresenter diagramPresenter, int controlPointX, int controlPointY, int x, int y) {
			if (diagramPresenter == null) throw new ArgumentNullException("display");
			return Geometry.DistancePointPoint(controlPointX, controlPointY, x, y) <= diagramPresenter.ZoomedGripSize;
		}


		/// <summary>
		/// Current state of the mouse (state after the last ProcessMouseEvent call).
		/// Position is in Diagram coordinates.
		/// </summary>
		protected MouseState CurrentMouseState {
			get { return currentMouseState; }
		}


		/// <summary>
		/// The display used by the current (pending) action.
		/// </summary>
		protected IDiagramPresenter ActionDiagramPresenter {
			get {
				if (pendingActions.Count == 0) throw new NShapeException("The action's current display was not set yet. Call StartToolAction method to set the action's current display.");
				else return pendingActions.Peek().DiagramPresenter;
			}
		}


		/// <summary>
		/// Transformed start coordinates of the current (pending) action (diagram coordinates).
		/// Use SetActionStartPosition method to set this value and ClearActionStartPosition to clear it.
		/// </summary>
		protected MouseState ActionStartMouseState {
			get {
				if (pendingActions.Count == 0) throw new NShapeInternalException("The action's start mouse state was not set yet. Call SetActionStartPosition method to set the start position.");
				else return pendingActions.Peek().MouseState;
			}
		}


		protected ActionDef CurrentToolAction {
			get {
				if (pendingActions.Count > 0)
					return pendingActions.Peek();
				else return ActionDef.Empty;
			}
		}
		
		
		protected IEnumerable<ActionDef> PendingToolActions {
			get { return pendingActions; }
		}


		protected int PendingToolActionsCount {
			get { return pendingActions.Count; }
		}


		protected ToolExecutedEventArgs ExecutedEventArgs;


		protected ToolExecutedEventArgs CancelledEventArgs;


		protected bool CanConnectTo(Shape shape, ControlPointId gluePointId, Shape targetShape) {
			if (shape == null) throw new ArgumentNullException("shape");
			if (targetShape == null) throw new ArgumentNullException("targetShape");
			// Connecting to a shape via Pointpto-shape connection is not allowed for both ends
			return (shape.IsConnected(ControlPointId.Any, targetShape) != ControlPointId.Reference);

			//if (shape is ILinearShape && ((ILinearShape)shape).VertexCount == 2) {
			//   foreach (ShapeConnectionInfo sci in shape.GetConnectionInfos(ControlPointId.Any, null)) {
			//      if (sci.OwnPointId != gluePointId 
			//         && sci.OtherShape == targetShape
			//         && (sci.OtherPointId == ControlPointId.Reference 
			//            || gluePointId == ControlPointId.Reference))
			//               return false;
			//   }
			//}
			//return true;
		}


		protected bool CanConnectTo(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId unmovedGluePoint, ControlPointId movedControlPoint, bool onlyUnselected) {
			if (shape is ILinearShape && ((ILinearShape)shape).VertexCount == 2) {
				Point posA = shape.GetControlPointPosition(unmovedGluePoint);
				Point posB = shape.GetControlPointPosition(movedControlPoint);
				ShapeAtCursorInfo shapeInfoA = FindShapeAtCursor(diagramPresenter, posA.X, posA.Y, ControlPointCapabilities.All, diagramPresenter.ZoomedGripSize, onlyUnselected);
				ShapeAtCursorInfo shapeInfoB = FindShapeAtCursor(diagramPresenter, posB.X, posB.Y, ControlPointCapabilities.All, diagramPresenter.ZoomedGripSize, onlyUnselected);
				if (!shapeInfoA.IsEmpty
					&& shapeInfoA.Shape == shapeInfoB.Shape
					&& (shapeInfoA.ControlPointId == ControlPointId.Reference
						|| shapeInfoB.ControlPointId == ControlPointId.Reference))
					return false;
			}
			return true;
		}


		/// <summary>
		/// Find the topmost shape that is not selected and has a valid ConnectionPoint (or ReferencePoint) 
		/// in range of the given point.
		/// </summary>
		private ShapeAtCursorInfo DoFindConnectionTarget(IDiagramPresenter diagramPresenter, int x, int y, bool onlyUnselected) {
			return DoFindConnectionTarget(diagramPresenter, null, ControlPointId.None, x, y, onlyUnselected);
		}


		/// <summary>
		/// Find the topmost shape that is not selected and has a valid ConnectionPoint (or ReferencePoint) 
		/// in range of the given point.
		/// </summary>
		private ShapeAtCursorInfo DoFindConnectionTarget(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId gluePointId, int x, int y, bool onlyUnselected) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			// Find (non-selected shape) its connection point under cursor
			ShapeAtCursorInfo result = ShapeAtCursorInfo.Empty;
			int resultZOrder = int.MinValue;
			if (diagramPresenter.Diagram != null) {
				foreach (Shape s in diagramPresenter.Diagram.Shapes.FindShapes(x, y, ControlPointCapabilities.Connect, diagramPresenter.ZoomedGripSize)) {
					if (s == shape) continue;
					// Skip shapes below the last matching shape
					if (s.ZOrder < resultZOrder) continue;
					// If the shape is already connected to the found shape via point-to-shape connection
					if (shape != null) {
						if(!CanConnectTo(shape, gluePointId, s)) continue;
						if (!CanConnectTo(diagramPresenter, shape,
							(gluePointId == ControlPointId.FirstVertex) ? ControlPointId.LastVertex : ControlPointId.FirstVertex,
							gluePointId, onlyUnselected)) continue;
					}
					// Skip selected shapes (if not wanted)
					if (onlyUnselected && diagramPresenter.SelectedShapes.Contains(s)) continue;
					// Perform a HitTest on the shape
					ControlPointId pointId = s.HitTest(x, y, ControlPointCapabilities.Connect, diagramPresenter.ZoomedGripSize);
					if (pointId != ControlPointId.None) {
						if (s.HasControlPointCapability(pointId, ControlPointCapabilities.Glue)) { continue; }
						result.Shape = s;
						result.ControlPointId = pointId;
						resultZOrder = s.ZOrder;
					}
				}
			}
			return result;
		}


		#region [Protected] Types

		protected struct MouseState {

			public static bool operator ==(MouseState a, MouseState b) {
				return (a.Position == b.Position
					&& a.Modifiers == b.Modifiers
					&& a.Buttons == b.Buttons);
			}

			public static bool operator !=(MouseState a, MouseState b) {
				return !(a == b);
			}

			public static MouseState Empty;

			public override int GetHashCode() {
				return Position.GetHashCode() ^ Buttons.GetHashCode() ^ Modifiers.GetHashCode();
			}

			public override bool Equals(object obj) {
				return (obj is MouseState && object.ReferenceEquals(this, obj));
			}

			public int X {
				get { return Position.X; }
			}

			public int Y {
				get { return Position.Y; }
			}

			public Point Position;

			public KeysDg Modifiers;

			public MouseButtonsDg Buttons;

			public bool IsButtonDown(MouseButtonsDg button) {
				return (Buttons & button) != 0;
			}

			public bool IsKeyPressed(KeysDg modifier) {
				return (Modifiers & modifier) != 0;
			}

			public bool IsEmpty {
				get { return this == Empty; }
			}

			static MouseState() {
				Empty.Position = Geometry.InvalidPoint;
				Empty.Modifiers = KeysDg.None;
				Empty.Buttons = 0;
			}
		}


		protected struct ShapeAtCursorInfo {

			public static bool operator ==(ShapeAtCursorInfo a, ShapeAtCursorInfo b) {
				return (a.Shape == b.Shape
					&& a.ControlPointId == b.ControlPointId
					&& a.CaptionIndex == b.CaptionIndex);
			}

			public static bool operator !=(ShapeAtCursorInfo a, ShapeAtCursorInfo b) {
				return !(a == b);
			}

			public static ShapeAtCursorInfo Empty;

			public override int GetHashCode() {
				return Shape.GetHashCode() ^ ControlPointId.GetHashCode() ^ CaptionIndex.GetHashCode();
			}

			public override bool Equals(object obj) {
				return (obj is ShapeAtCursorInfo && object.ReferenceEquals(this, obj));
			}

			public void Clear() {
				this = Empty;
			}

			public Shape Shape;

			public ControlPointId ControlPointId;

			public int CaptionIndex;

			public bool IsCursorAtGrip {
				get {
					return (Shape != null
					&& ControlPointId != ControlPointId.None
					&& ControlPointId != ControlPointId.Reference);
				}
			}

			public bool IsCursorAtGluePoint {
				get {
					return (Shape != null
						&& Shape.HasControlPointCapability(ControlPointId, ControlPointCapabilities.Glue));
				}
			}

			public bool IsCursorAtConnectionPoint {
				get {
					return (Shape != null
						&& Shape.HasControlPointCapability(ControlPointId, ControlPointCapabilities.Connect));
				}
			}

			public bool IsCursorAtCaption {
				get { return (Shape is ICaptionedShape && CaptionIndex >= 0 && !IsCursorAtGrip); }
			}

			public bool IsEmpty {
				get { return Shape == null; }
			}

			static ShapeAtCursorInfo() {
				Empty.Shape = null;
				Empty.ControlPointId = ControlPointId.None;
				Empty.CaptionIndex = -1;
			}
		}


		protected struct ActionDef {

			public static readonly ActionDef Empty;

			public static ActionDef Create(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantsAutoScroll) {
				ActionDef result = ActionDef.Empty;
				result.diagramPresenter = diagramPresenter;
				result.action = action;
				result.mouseState = mouseState;
				result.wantsAutoScroll = wantsAutoScroll;
				return result;
			}


			public ActionDef(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantsAutoScroll) {
				this.diagramPresenter = diagramPresenter;
				this.action = action;
				this.mouseState = mouseState;
				this.wantsAutoScroll = wantsAutoScroll;
			}


			public IDiagramPresenter DiagramPresenter {
				get { return diagramPresenter; }
			}


			public MouseState MouseState {
				get { return mouseState; }
			}


			public int Action {
				get { return action; }
			}


			public bool WantsAutoScroll {
				get { return wantsAutoScroll; }
			}


			static ActionDef() {
				Empty.diagramPresenter = null;
				Empty.action = int.MinValue;
				Empty.mouseState = MouseState.Empty;
				Empty.wantsAutoScroll = false;
			}


			private IDiagramPresenter diagramPresenter;
			private MouseState mouseState;
			private int action;
			private bool wantsAutoScroll;
		}

		#endregion


		#region Fields

		// --- Description of the tool ---
		// Unique name of the tool.
		private string name;
		// Title that will be displayed in the tool box
		private string title;
		// Category title of the tool, used for grouping tools in the tool box
		private string category;
		// Hint that will be displayed when the mouse is hovering the tool
		private string description;
		// small icon of the tool
		private Bitmap smallIcon;
		// the large icon of the tool
		private Bitmap largeIcon;
		//
		// margin and background colors of the toolbox icons "LargeIcon" and "SmallIcon"
		protected int margin = 1;
		protected Color transparentColor = Color.LightGray;
		// highlighting connection targets in range
		private int pointHighlightRange = 50;
		//
		// --- Mouse state after last mouse event ---
		// State of the mouse after the last ProcessMouseEvents call
		private MouseState currentMouseState = MouseState.Empty;
		// Shapes whose connection points will be highlighted in the next drawing
		private List<Shape> shapesInRange = new List<Shape>();

		// --- Definition of current action(s) ---
		// The stack contains 
		// - the display that is edited with this tool,
		// - transformed coordinates of the mouse position when an action has started (diagram coordinates)
		private Stack<ActionDef> pendingActions = new Stack<ActionDef>(1);
		// 
		// Work buffer for shapes
		private List<Shape> shapeBuffer = new List<Shape>();

		#endregion
	}


	/// <summary>
	/// Lets the user size, move, rotate and select shapes.
	/// </summary>
	public class PointerTool : Tool {

		public PointerTool()
			: base("Standard") {
			Construct();
		}


		public PointerTool(string category)
			: base(category) {
			Construct();
		}


		#region [Public] Tool Members

		/// <override></override>
		public override void RefreshIcons() {
			// nothing to do...
		}


		/// <override></override>
		public override bool ProcessMouseEvent(IDiagramPresenter diagramPresenter, MouseEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			bool result = false;
			// get new mouse state
			MouseState newMouseState = MouseState.Empty;
			newMouseState.Buttons = e.Buttons;
			newMouseState.Modifiers = e.Modifiers;
			diagramPresenter.ControlToDiagram(e.Position, out newMouseState.Position);

			diagramPresenter.SuspendUpdate();
			try {
				// Only process mouse action if the position of the mouse or a mouse button state changed
				if (e.EventType != MouseEventType.MouseMove || newMouseState.Position != CurrentMouseState.Position) {
					// Process the mouse event
					switch (e.EventType) {
						case MouseEventType.MouseDown:
							// Start drag action such as drawing a SelectionFrame or moving selectedShapes/shape handles
							result = ProcessMouseDown(diagramPresenter, newMouseState);
							break;

						case MouseEventType.MouseMove:
							// Set cursors depending on HotSpots or draw moving/resizing preview
							result = ProcessMouseMove(diagramPresenter, newMouseState);
							break;

						case MouseEventType.MouseUp:
							// perform selection/moving/resizing
							result = ProcessMouseUp(diagramPresenter, newMouseState);
							if (!result && e.Clicks > 1)
								// perform QuickRotate (90�) if the feature is enabled
								result = ProcessDoubleClick(diagramPresenter, newMouseState, e.Clicks);
							break;

						default: throw new NShapeUnsupportedValueException(e.EventType);
					}
				}
			} finally { diagramPresenter.ResumeUpdate(); }
			base.ProcessMouseEvent(diagramPresenter, e);
			return result;
		}


		/// <override></override>
		public override bool ProcessKeyEvent(IDiagramPresenter diagramPresenter, KeyEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			bool result = base.ProcessKeyEvent(diagramPresenter, e);
			// if the keyPress was not handled by the base class, try to handle it here
			if (!result) {
				switch (e.EventType) {
					case KeyEventType.PreviewKeyDown:
					case KeyEventType.KeyPress:
						// do nothing
						break;
					case KeyEventType.KeyDown:
					case KeyEventType.KeyUp:
						if (((KeysDg)e.KeyCode & KeysDg.Shift) == KeysDg.Shift
							|| ((KeysDg)e.KeyCode & KeysDg.ShiftKey) == KeysDg.ShiftKey
							|| ((KeysDg)e.KeyCode & KeysDg.Control) == KeysDg.Control
							|| ((KeysDg)e.KeyCode & KeysDg.ControlKey) == KeysDg.ControlKey
							|| ((KeysDg)e.KeyCode & KeysDg.Alt) == KeysDg.Alt) {
							MouseState mouseState = CurrentMouseState;
							mouseState.Modifiers = (KeysDg)e.Modifiers;
							int cursorId = DetermineCursor(diagramPresenter, mouseState);
							diagramPresenter.SetCursor(cursorId);
						}
						break;
					default: throw new NShapeUnsupportedValueException(e.EventType);
				}
			}
			return result;
		}


		/// <override></override>
		public override void EnterDisplay(IDiagramPresenter diagramPresenter) {
			// nothing to do
		}


		/// <override></override>
		public override void LeaveDisplay(IDiagramPresenter diagramPresenter) {
			// nothing to do
		}


		/// <override></override>
		public override IEnumerable<MenuItemDef> GetMenuItemDefs(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			int mouseX = CurrentMouseState.X;
			int mouseY = CurrentMouseState.Y;

			bool separatorRequired = false;

			// Return the shape's actions
			if (diagramPresenter.SelectedShapes.Count == 1 && !SelectedShapeAtCursorInfo.IsEmpty) {
				// ToDo: Create an aggregated command that creates a composite shape first and then a template from it
				if (SelectedShapeAtCursorInfo.Shape.Template != null) {
					// Deliver Template's actions
					foreach (MenuItemDef action in SelectedShapeAtCursorInfo.Shape.Template.GetMenuItemDefs()) {
						if (!separatorRequired) separatorRequired = true;
						yield return action;
					}
				}
				foreach (MenuItemDef action in SelectedShapeAtCursorInfo.Shape.GetMenuItemDefs(mouseX, mouseY, diagramPresenter.ZoomedGripSize)) {
					if (separatorRequired) yield return new SeparatorMenuItemDef();
					yield return action;
				}
				if (SelectedShapeAtCursorInfo.Shape.ModelObject != null) {
					if (separatorRequired) yield return new SeparatorMenuItemDef();
					foreach (MenuItemDef action in SelectedShapeAtCursorInfo.Shape.ModelObject.GetMenuItemDefs())
						yield return action;
				}
			} else {
				// ToDo: Find shape under the cursor and return its actions?
				// ToDo: Collect all actions provided by the diagram if no shape was right-clicked
			}
			// ToDo: Add tool-specific actions?
		}


		/// <override></override>
		public override void Draw(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			switch (CurrentAction) {
				case Action.Select:
					// nothing to do
					break;

				case Action.None:
				case Action.EditCaption:
					// MouseOver-Highlighting of the caption under the cursor 
					// At the moment, the ownerDisplay draws the caption bounds along with the selection highlighting
					IDiagramPresenter presenter = (CurrentAction == Action.None) ? diagramPresenter : ActionDiagramPresenter;
					if (IsEditCaptionFeasible(presenter, CurrentMouseState, SelectedShapeAtCursorInfo)) {
						diagramPresenter.ResetTransformation();
						try {
							diagramPresenter.DrawCaptionBounds(IndicatorDrawMode.Highlighted, (ICaptionedShape)SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.CaptionIndex);
						} finally { diagramPresenter.RestoreTransformation(); }
					}
					break;

				case Action.SelectWithFrame:
					diagramPresenter.ResetTransformation();
					try {
						diagramPresenter.DrawSelectionFrame(frameRect);
					} finally { diagramPresenter.RestoreTransformation(); }
					break;

				case Action.MoveShape:
				case Action.MoveHandle:
					// Draw shape previews first
					diagramPresenter.DrawShapes(Previews.Values);

					// Then draw snap-lines and -points
					if (SelectedShapeAtCursorInfo != null && (snapPtId > 0 || snapDeltaX != 0 || snapDeltaY != 0)) {
						Shape previewAtCursor = FindPreviewOfShape(SelectedShapeAtCursorInfo.Shape);
						diagramPresenter.DrawSnapIndicators(previewAtCursor);
					}
					// Finally, draw highlighten ConnectionPoints and/or highlighted shape outlines
					if (Previews.Count == 1 && SelectedShapeAtCursorInfo.ControlPointId != ControlPointId.None) {
						Shape preview = null;
						foreach (KeyValuePair<Shape, Shape> item in Previews) {
							preview = item.Value;
							break;
						}
						if (preview.HasControlPointCapability(SelectedShapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Glue)) {
							// Find and highlight valid connection targets in range
							Point p = preview.GetControlPointPosition(SelectedShapeAtCursorInfo.ControlPointId);
							DrawConnectionTargets(ActionDiagramPresenter, SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, p, ActionDiagramPresenter.SelectedShapes);
						}
					}
					break;

				case Action.PrepareRotate:
				case Action.Rotate:
					if (CurrentAction == Action.Rotate)
						diagramPresenter.DrawShapes(Previews.Values);
					diagramPresenter.ResetTransformation();
					try {
						if (PendingToolActionsCount == 1) {
							diagramPresenter.DrawAnglePreview(rectBuffer.Location, rectBuffer.Width, CurrentMouseState.Position, cursors[ToolCursor.Rotate], 0, 0);
						} else {
							// Get MouseState of the first click (on the rotation point)
							MouseState initMouseState = GetPreviousMouseState();
							int startAngle, sweepAngle;
							CalcAngle(initMouseState, ActionStartMouseState, CurrentMouseState, out startAngle, out sweepAngle);

							// ToDo: Determine standard cursor size
							rectBuffer.Location = SelectedShapeAtCursorInfo.Shape.GetControlPointPosition(SelectedShapeAtCursorInfo.ControlPointId);
							rectBuffer.Width = rectBuffer.Height = (int)Math.Ceiling(Geometry.DistancePointPoint(rectBuffer.Location, CurrentMouseState.Position));

							diagramPresenter.DrawAnglePreview(rectBuffer.Location, rectBuffer.Width, CurrentMouseState.Position,
								cursors[ToolCursor.Rotate], startAngle, sweepAngle);
						}
					} finally { diagramPresenter.RestoreTransformation(); }
					break;

				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}
		}


		/// <override></override>
		public override void Invalidate(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			switch (CurrentAction) {
				case Action.None:
				case Action.Select:
				case Action.EditCaption:
					if (!SelectedShapeAtCursorInfo.IsEmpty) {
						SelectedShapeAtCursorInfo.Shape.Invalidate();
						diagramPresenter.InvalidateGrips(SelectedShapeAtCursorInfo.Shape, ControlPointCapabilities.All);
					}
					break;

				case Action.SelectWithFrame:
					diagramPresenter.DisplayService.Invalidate(frameRect);
					break;

				case Action.MoveHandle:
				case Action.MoveShape:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					if (Previews.Count > 0) {
						InvalidateShapes(diagramPresenter, Previews.Values);
						if (diagramPresenter.SnapToGrid) {
							Shape previewAtCursor = FindPreviewOfShape(SelectedShapeAtCursorInfo.Shape);
							diagramPresenter.InvalidateSnapIndicators(previewAtCursor);
						}
						if (CurrentAction == Action.MoveHandle && SelectedShapeAtCursorInfo.IsCursorAtGluePoint)
							InvalidateConnectionTargets(diagramPresenter, CurrentMouseState.X, CurrentMouseState.Y);
					}
					break;

				case Action.PrepareRotate:
				case Action.Rotate:
					if (Previews.Count > 0) InvalidateShapes(diagramPresenter, Previews.Values);
					InvalidateAnglePreview(diagramPresenter);
					break;

				default: throw new NShapeUnsupportedValueException(typeof(MenuItemDef), CurrentAction);
			}
		}


		/// <override></override>
		protected override void CancelCore() {
			frameRect = Rectangle.Empty;
			rectBuffer = Rectangle.Empty;

			//currentToolAction = ToolAction.None;
			SelectedShapeAtCursorInfo.Clear();
		}

		#endregion


		#region [Protected] Tool Members

		/// <override></override>
		protected override void StartToolAction(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantAutoScroll) {
			base.StartToolAction(diagramPresenter, action, mouseState, wantAutoScroll);
			// Empty selection frame
			frameRect.Location = mouseState.Position;
			frameRect.Size = Size.Empty;
		}


		/// <override></override>
		protected override void EndToolAction() {
			base.EndToolAction();
			//currentToolAction = ToolAction.None;
			if (!IsToolActionPending)
				ClearPreviews();
		}

		#endregion


		#region [Private] Properties

		private Action CurrentAction {
			get {
				//return currentToolAction; 
				if (IsToolActionPending)
					return (Action)CurrentToolAction.Action;
				else return Action.None;
			}
		}


		private ShapeAtCursorInfo SelectedShapeAtCursorInfo {
			get { return selShapeAtCursorInfo; }
		}

		#endregion


		#region [Private] MouseEvent processing implementation

		private bool ProcessMouseDown(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;

			if (!SelectedShapeAtCursorInfo.IsEmpty &&
				!diagramPresenter.SelectedShapes.Contains(SelectedShapeAtCursorInfo.Shape))
				SelectedShapeAtCursorInfo.Clear();

			// If no action is pending, try to start a new one...
			if (CurrentAction == Action.None) {
				// Get suitable action (depending on the currently selected shape under the mouse cursor)
				Action newAction = DetermineMouseDownAction(diagramPresenter, mouseState);
				if (newAction != Action.None) {
					//currentToolAction = newAction;
					bool wantAutoScroll;
					switch (newAction) {
						case Action.SelectWithFrame:
						case Action.MoveHandle:
						case Action.MoveShape:
							wantAutoScroll = true; break;
						default: wantAutoScroll = false; break;
					}
					StartToolAction(diagramPresenter, (int)newAction, mouseState, wantAutoScroll);

					// If the action requires preview shapes, create them now...
					switch (CurrentAction) {
						case Action.None:
						case Action.Select:
						case Action.SelectWithFrame:
						case Action.EditCaption:
							break;
						case Action.MoveHandle:
						case Action.MoveShape:
						case Action.PrepareRotate:
						case Action.Rotate:
							CreatePreviewShapes(diagramPresenter);
							break;
						default: throw new NShapeUnsupportedValueException(CurrentAction);
					}

					Invalidate(ActionDiagramPresenter);
					result = true;
				}
			} else {
				// ... otherwise cancel the action (if right mouse button was pressed)
				Action newAction = DetermineMouseDownAction(diagramPresenter, mouseState);
				if (newAction == Action.None) {
					Cancel();
					result = true;
				}
			}
			return result;
		}


		private bool ProcessMouseMove(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;

			if (!SelectedShapeAtCursorInfo.IsEmpty &&
				!diagramPresenter.SelectedShapes.Contains(SelectedShapeAtCursorInfo.Shape))
				SelectedShapeAtCursorInfo.Clear();

			Action newAction;
			switch (CurrentAction) {
				case Action.None:
					SetSelectedShapeAtCursor(diagramPresenter, mouseState.X, mouseState.Y, diagramPresenter.ZoomedGripSize, ControlPointCapabilities.All);
					Invalidate(diagramPresenter);
					break;

				case Action.EditCaption:
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.Select:
					// Find unselected shape under the mouse cursor
					ShapeAtCursorInfo shapeAtCursorInfo = ShapeAtCursorInfo.Empty;
					newAction = CurrentAction;
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						shapeAtCursorInfo = FindShapeAtCursor(ActionDiagramPresenter, ActionStartMouseState.X, ActionStartMouseState.Y, ControlPointCapabilities.None, 0, true);
						newAction = DetermineMouseMoveAction(ActionDiagramPresenter, ActionStartMouseState, shapeAtCursorInfo);
					} else newAction = DetermineMouseMoveAction(ActionDiagramPresenter, ActionStartMouseState, shapeAtCursorInfo);

					// If the action has changed, prepare and start the new action
					if (newAction != CurrentAction) {
						switch (newAction) {
							// Select -> SelectWithFrame
							case Action.SelectWithFrame:
								Assert(CurrentAction == Action.Select);
								StartToolAction(diagramPresenter, (int)newAction, ActionStartMouseState, true);
								result = PrepareSelectionFrame(ActionDiagramPresenter, ActionStartMouseState);
								break;

							// Select -> (Select and) move shape
							case Action.MoveShape:
								Assert(CurrentAction == Action.Select);
								if (SelectedShapeAtCursorInfo.IsEmpty) {
									// Select shape at cursor before start dragging it
									PerformSelection(ActionDiagramPresenter, ActionStartMouseState, shapeAtCursorInfo);
									SetSelectedShapeAtCursor(diagramPresenter, ActionStartMouseState.X, ActionStartMouseState.Y, 0, ControlPointCapabilities.None);
									Assert(!SelectedShapeAtCursorInfo.IsEmpty);
								}
								// Init moving shape
								Assert(!SelectedShapeAtCursorInfo.IsEmpty);
								CreatePreviewShapes(ActionDiagramPresenter);
								StartToolAction(diagramPresenter, (int)newAction, ActionStartMouseState, true);
								result = PrepareMoveShapePreview(ActionDiagramPresenter, ActionStartMouseState);
								break;

							case Action.PrepareRotate:
							case Action.Rotate:
							case Action.EditCaption:
							case Action.MoveHandle:
							case Action.None:
							case Action.Select:
								Debug.Fail("Unhandled change of CurrentAction.");
								break;
							default:
								Debug.Fail(string.Format("Unexpected {0} value: {1}", CurrentAction.GetType().Name, CurrentAction));
								break;
						}
						//currentToolAction = newAction;
					}
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.SelectWithFrame:
					Invalidate(ActionDiagramPresenter);
					result = PrepareSelectionFrame(ActionDiagramPresenter, mouseState);
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.MoveHandle:
					Assert(IsMoveHandleFeasible(ActionDiagramPresenter, SelectedShapeAtCursorInfo));
					Invalidate(ActionDiagramPresenter);
					result = PrepareMoveHandlePreview(ActionDiagramPresenter, mouseState);
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.MoveShape:
					Invalidate(ActionDiagramPresenter);
					result = PrepareMoveShapePreview(diagramPresenter, mouseState);
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.PrepareRotate:
				case Action.Rotate:
					Assert(IsRotatatingFeasible(ActionDiagramPresenter, SelectedShapeAtCursorInfo));
					newAction = CurrentAction;
					// Find unselected shape under the mouse cursor
					newAction = DetermineMouseMoveAction(ActionDiagramPresenter, mouseState, SelectedShapeAtCursorInfo);

					// If the action has changed, prepare and start the new action
					if (newAction != CurrentAction) {
						switch (newAction) {
							// Rotate shape -> Prepare shape rotation
							case Action.PrepareRotate:
								Assert(CurrentAction == Action.Rotate);
								EndToolAction();
								ClearPreviews();
								break;

							// Prepare shape rotation -> Rotate shape
							case Action.Rotate:
								Assert(CurrentAction == Action.PrepareRotate);
								StartToolAction(ActionDiagramPresenter, (int)newAction, mouseState, false);
								CreatePreviewShapes(ActionDiagramPresenter);
								break;

							case Action.SelectWithFrame:
							case Action.MoveShape:
							case Action.EditCaption:
							case Action.MoveHandle:
							case Action.None:
							case Action.Select:
								Debug.Fail("Unhandled change of CurrentAction.");
								break;
							default:
								Debug.Fail(string.Format("Unexpected {0} value: {1}", CurrentAction.GetType().Name, CurrentAction));
								break;
						}
						//currentToolAction = newAction;
					}

					Invalidate(ActionDiagramPresenter);
					PrepareRotatePreview(ActionDiagramPresenter, mouseState);
					Invalidate(ActionDiagramPresenter);
					break;

				default: throw new NShapeUnsupportedValueException(typeof(Action), CurrentAction);
			}

			int cursorId = DetermineCursor(diagramPresenter, mouseState);
			if (CurrentAction == Action.None) diagramPresenter.SetCursor(cursorId);
			else ActionDiagramPresenter.SetCursor(cursorId);

			return result;
		}


		private bool ProcessMouseUp(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;

			if (!SelectedShapeAtCursorInfo.IsEmpty &&
				!diagramPresenter.SelectedShapes.Contains(SelectedShapeAtCursorInfo.Shape))
				SelectedShapeAtCursorInfo.Clear();

			switch (CurrentAction) {
				case Action.None:
					// do nothing
					break;

				case Action.Select:
					// Perform selection
					ShapeAtCursorInfo shapeAtCursorInfo = ShapeAtCursorInfo.Empty;
					if (!SelectedShapeAtCursorInfo.IsEmpty) {
						if (SelectedShapeAtCursorInfo.Shape.ContainsPoint(mouseState.X, mouseState.Y)) {
							Shape shape = ActionDiagramPresenter.Diagram.Shapes.FindShape(mouseState.X, mouseState.Y, ControlPointCapabilities.None, 0, SelectedShapeAtCursorInfo.Shape);
							if (shape != null) {
								shapeAtCursorInfo.Shape = shape;
								shapeAtCursorInfo.ControlPointId = shape.HitTest(mouseState.X, mouseState.Y, ControlPointCapabilities.None, 0);
								shapeAtCursorInfo.CaptionIndex = -1;
							}
						}
					} else shapeAtCursorInfo = FindShapeAtCursor(diagramPresenter, mouseState.X, mouseState.Y, ControlPointCapabilities.None, 0, false);
					result = PerformSelection(ActionDiagramPresenter, mouseState, shapeAtCursorInfo);

					SetSelectedShapeAtCursor(ActionDiagramPresenter, mouseState.X, mouseState.Y, ActionDiagramPresenter.ZoomedGripSize, ControlPointCapabilities.All);
					EndToolAction();
					break;

				case Action.SelectWithFrame:
					// select all selectedShapes within the frame
					result = PerformFrameSelection(ActionDiagramPresenter, mouseState);
					while (IsToolActionPending)
						EndToolAction();
					break;

				case Action.EditCaption:
					// if the user clicked a caption, display the caption editor
					Assert(SelectedShapeAtCursorInfo.IsCursorAtCaption);
					ActionDiagramPresenter.OpenCaptionEditor((ICaptionedShape)SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.CaptionIndex);
					EndToolAction();
					result = true;
					break;

				case Action.MoveHandle:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					result = PerformMoveHandle(ActionDiagramPresenter, mouseState);
					while (IsToolActionPending)
						EndToolAction();
					break;

				case Action.MoveShape:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					result = PerformMoveShape(ActionDiagramPresenter, mouseState);
					while (IsToolActionPending)
						EndToolAction();
					break;

				case Action.PrepareRotate:
					result = true;
					EndToolAction();
					break;

				case Action.Rotate:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					result = PerformRotate(ActionDiagramPresenter, mouseState);
					while (IsToolActionPending)
						EndToolAction();
					break;

				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}

			SetSelectedShapeAtCursor(diagramPresenter, mouseState.X, mouseState.Y, diagramPresenter.ZoomedGripSize, ControlPointCapabilities.All);
			diagramPresenter.SetCursor(DetermineCursor(diagramPresenter, mouseState));

			OnToolExecuted(ExecutedEventArgs);
			return result;
		}


		private bool ProcessDoubleClick(IDiagramPresenter diagramPresenter, MouseState mouseState, int clickCount) {
			bool result = false;
			if (diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout, diagramPresenter.SelectedShapes) && enableQuickRotate) {
				if (!SelectedShapeAtCursorInfo.IsEmpty && SelectedShapeAtCursorInfo.IsCursorAtGrip
					&& SelectedShapeAtCursorInfo.Shape.HasControlPointCapability(SelectedShapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Rotate)) {
					int angle = 900 * (clickCount - 1);
					if (angle % 3600 != 0) {
						PerformQuickRotate(diagramPresenter, angle);
						result = true;
						OnToolExecuted(ExecutedEventArgs);
					}
				}
			}
			return result;
		}

		#endregion


		#region [Private] Determine action depending on mouse state and event type

		/// <summary>
		/// Decide which tool action is suitable for the current mouse state.
		/// </summary>
		private Action DetermineMouseDownAction(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
				if (!SelectedShapeAtCursorInfo.IsEmpty) {
					// Check if cursor is over a control point and moving grips or rotating is feasible
					if (SelectedShapeAtCursorInfo.IsCursorAtGrip) {
						if (IsMoveHandleFeasible(diagramPresenter, SelectedShapeAtCursorInfo))
							return Action.MoveHandle;
						else if (IsRotatatingFeasible(diagramPresenter, SelectedShapeAtCursorInfo))
							return Action.PrepareRotate;
					}
					// Moving shapes is initiated as soon as the user starts drag action (move mouse 
					// while mouse button is pressed) 
					// If the user does not start a drag action, this will result in (un)selecting shapes.
				}
				// If the cursor is not over a caption of a selected shape when clicking left mouse button, 
				// we assume the user wants to select something
				// Same thing if no other action is granted.
				if (IsEditCaptionFeasible(diagramPresenter, mouseState, SelectedShapeAtCursorInfo))
					return Action.EditCaption;
				else return Action.Select;
			} else if (mouseState.IsButtonDown(MouseButtonsDg.Right)) {
				// Abort current action when clicking right mouse button
				return Action.None;
			} else {
				// Ignore other pressed mouse buttons
				return CurrentAction;
			}
		}


		/// <summary>
		/// Decide which tool action is suitable for the current mouse state.
		/// </summary>
		private Action DetermineMouseMoveAction(IDiagramPresenter diagramPresenter, MouseState mouseState, ShapeAtCursorInfo shapeAtCursorInfo) {
			switch (CurrentAction) {
				case Action.None:
				case Action.EditCaption:
				case Action.MoveHandle:
				case Action.MoveShape:
				case Action.SelectWithFrame:
					// Do not change the current action
					return CurrentAction;

				case Action.Select:
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						// If there is no shape under the cursor, start a SelectWithFrame action,
						// otherwise start a MoveShape action
						if (IsMoveShapeFeasible(diagramPresenter, mouseState, SelectedShapeAtCursorInfo)
							|| IsMoveShapeFeasible(diagramPresenter, mouseState, shapeAtCursorInfo))
							return Action.MoveShape;
						else return Action.SelectWithFrame;
					} else return CurrentAction;

				case Action.PrepareRotate:
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						// If the mouse has left the min rotate range, start 'real' rotating
						if (IsMinRotateRangeExceeded(diagramPresenter, mouseState))
							return Action.Rotate;
						else return CurrentAction;
					} else return CurrentAction;

				case Action.Rotate:
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						// If the mouse has entered the min rotate range, start showing rotating hint
						if (!IsMinRotateRangeExceeded(diagramPresenter, mouseState))
							return Action.PrepareRotate;
						else return CurrentAction;
					} else return CurrentAction;

				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}
		}

		#endregion


		#region [Private] Action implementations

		#region Selecting Shapes

		// (Un)Select shape unter the mouse pointer
		private bool PerformSelection(IDiagramPresenter diagramPresenter, MouseState mouseState, ShapeAtCursorInfo shapeAtCursorInfo) {
			bool result = false;
			bool multiSelect = mouseState.IsKeyPressed(KeysDg.Control) || mouseState.IsKeyPressed(KeysDg.Shift);

			// When selecting shapes conteolpoints should be ignored as the user does not see them 
			// until a shape is selected
			const ControlPointCapabilities capabilities = ControlPointCapabilities.None;
			const int range = 0;

			// Determine the shape that has to be selected:
			Shape shapeToSelect = null;
			if (!SelectedShapeAtCursorInfo.IsEmpty) {
				// When in multiSelection mode, unselect the selected shape under the cursor
				if (multiSelect) shapeToSelect = SelectedShapeAtCursorInfo.Shape;
				else {
					// First, check if the selected shape under the cursor has children that can be selected
					shapeToSelect = SelectedShapeAtCursorInfo.Shape.Children.FindShape(mouseState.X, mouseState.Y, capabilities, range, null);
					// Second, check if the selected shape under the cursor has siblings that can be selected
					if (shapeToSelect == null && SelectedShapeAtCursorInfo.Shape.Parent != null) {
						shapeToSelect = SelectedShapeAtCursorInfo.Shape.Parent.Children.FindShape(mouseState.X, mouseState.Y, capabilities, range, SelectedShapeAtCursorInfo.Shape);
						// Discard found shape if it is the selected shape at cursor
						if (shapeToSelect == SelectedShapeAtCursorInfo.Shape) shapeToSelect = null;
						if (shapeToSelect == null) {
							foreach (Shape shape in SelectedShapeAtCursorInfo.Shape.Parent.Children.FindShapes(mouseState.X, mouseState.Y, capabilities, range)) {
								if (shape == SelectedShapeAtCursorInfo.Shape) continue;
								shapeToSelect = shape;
								break;
							}
						}
					}
					// Third, check if there are non-selected shapes below the selected shape under the cursor
					Shape startShape = SelectedShapeAtCursorInfo.Shape;
					while (startShape.Parent != null) startShape = startShape.Parent;
					if (shapeToSelect == null && diagramPresenter.Diagram.Shapes.Contains(startShape))
						shapeToSelect = diagramPresenter.Diagram.Shapes.FindShape(mouseState.X, mouseState.Y, capabilities, range, startShape);
				}
			}

			// If there was a shape to select related to the selected shape under the cursor
			// (a child or a sibling of the selected shape or a shape below it),
			// try to select the first non-selected shape under the cursor
			if (shapeToSelect == null && shapeAtCursorInfo.Shape != null
				&& shapeAtCursorInfo.Shape.ContainsPoint(mouseState.X, mouseState.Y))
				shapeToSelect = shapeAtCursorInfo.Shape;

			// If a new shape to select was found, perform selection
			if (shapeToSelect != null) {
				// (check if multiselection mode is enabled (Shift + Click or Ctrl + Click))
				if (multiSelect) {
					// if multiSelect is enabled, add/remove to/from selected selectedShapes...
					if (diagramPresenter.SelectedShapes.Contains(shapeToSelect)) {
						// if object is selected -> remove from selection
						diagramPresenter.UnselectShape(shapeToSelect);
						RemovePreviewOf(shapeToSelect);
						result = true;
					} else {
						// If object is not selected -> add to selection
						diagramPresenter.SelectShape(shapeToSelect, true);
						result = true;
					}
				} else {
					// ... otherwise deselect all selectedShapes but the clicked object
					ClearPreviews();
					// check if the clicked shape is a child of an already selected shape
					Shape childShape = null;
					if (diagramPresenter.SelectedShapes.Count == 1
						&& diagramPresenter.SelectedShapes.TopMost.Children != null
						&& diagramPresenter.SelectedShapes.TopMost.Children.Count > 0) {
						childShape = diagramPresenter.SelectedShapes.TopMost.Children.FindShape(mouseState.X, mouseState.Y, ControlPointCapabilities.None, 0, null);
					}
					if (childShape != null) diagramPresenter.SelectShape(childShape, false);
					else diagramPresenter.SelectShape(shapeToSelect, false);
					result = true;
				}

				// validate if the desired shape or its parent was selected
				if (shapeToSelect.Parent != null) {
					if (!diagramPresenter.SelectedShapes.Contains(shapeToSelect))
						if (diagramPresenter.SelectedShapes.Contains(shapeToSelect.Parent))
							shapeToSelect = shapeToSelect.Parent;
				}
			} else if (SelectedShapeAtCursorInfo.IsEmpty) {
				// if there was no other shape to select and none of the selected shapes is under the cursor,
				// clear selection
				if (!multiSelect) {
					if (diagramPresenter.SelectedShapes.Count > 0) {
						diagramPresenter.UnselectAll();
						ClearPreviews();
					}
					result = true;
				}
			} else {
				// if there was no other shape to select and a selected shape is under the cursor,
				// do nothing
			}
			return result;
		}


		// Calculate new selection frame
		private bool PrepareSelectionFrame(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			frameRect.X = Math.Min(ActionStartMouseState.X, mouseState.X);
			frameRect.Y = Math.Min(ActionStartMouseState.Y, mouseState.Y);
			frameRect.Width = Math.Max(ActionStartMouseState.X, mouseState.X) - frameRect.X;
			frameRect.Height = Math.Max(ActionStartMouseState.Y, mouseState.Y) - frameRect.Y;
			return true;
		}


		// Select shapes inside the selection frame
		private bool PerformFrameSelection(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool multiSelect = mouseState.IsKeyPressed(KeysDg.Control) || mouseState.IsKeyPressed(KeysDg.Shift);
			diagramPresenter.SelectShapes(frameRect, multiSelect);
			return true;
		}

		#endregion


		#region Connecting / Disconnecting GluePoints

		private bool ShapeHasGluePoint(Shape shape) {
			foreach (ControlPointId id in shape.GetControlPointIds(ControlPointCapabilities.Glue))
				return true;
			return false;
		}


		private void DisconnectGluePoints(IDiagramPresenter diagramPresenter) {
			foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
				foreach (ControlPointId ptId in selectedShape.GetControlPointIds(ControlPointCapabilities.Connect | ControlPointCapabilities.Glue)) {
					// disconnect GluePoints if they are moved together with their targets
					bool skip = false;
					foreach (ShapeConnectionInfo ci in selectedShape.GetConnectionInfos(ptId, null)) {
						if (ci.OwnPointId != ptId) throw new NShapeInternalException("Fatal error: Unexpected ShapeConnectionInfo was returned.");
						if (diagramPresenter.SelectedShapes.Contains(ci.OtherShape)) {
							skip = false;
							break;
						}
					}
					if (skip) continue;

					// otherwise, compare positions of the GluePoint with it's targetPoint and disconnect if they are not equal
					if (selectedShape.HasControlPointCapability(ptId, ControlPointCapabilities.Glue)) {
						Shape previewShape = FindPreviewOfShape(selectedShape);
						if (selectedShape.GetControlPointPosition(ptId) != previewShape.GetControlPointPosition(ptId)) {
							bool isConnected = false;
							foreach (ShapeConnectionInfo sci in selectedShape.GetConnectionInfos(ptId, null)) {
								if (sci.OwnPointId == ptId) {
									isConnected = true;
									break;
								} else throw new NShapeInternalException("Fatal error: Unexpected ShapeConnectionInfo was returned.");
							}
							if (isConnected) {
								ICommand cmd = new DisconnectCommand(selectedShape, ptId);
								diagramPresenter.Project.ExecuteCommand(cmd);
							}
						}
					}
				}
			}
		}


		private void ConnectGluePoints(IDiagramPresenter diagramPresenter) {
			foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
				// find selectedShapes that own GluePoints
				foreach (ControlPointId gluePointId in selectedShape.GetControlPointIds(ControlPointCapabilities.Glue)) {
					Point gluePointPos = Point.Empty;
					gluePointPos = selectedShape.GetControlPointPosition(gluePointId);

					// find selectedShapes to connect to
					foreach (Shape shape in diagramPresenter.Diagram.Shapes.FindShapes(gluePointPos.X, gluePointPos.Y, ControlPointCapabilities.Connect, diagramPresenter.GripSize)) {
						if (diagramPresenter.SelectedShapes.Contains(shape)) {
							// restore connections that were disconnected before
							int targetPointId = shape.FindNearestControlPoint(gluePointPos.X, gluePointPos.Y, 0, ControlPointCapabilities.Connect);
							if (targetPointId != ControlPointId.None)
								selectedShape.Connect(gluePointId, shape, targetPointId);
						} else {
							ShapeAtCursorInfo shapeInfo = FindConnectionTarget(diagramPresenter, selectedShape, gluePointId, gluePointPos, true);
							if (shapeInfo.ControlPointId != ControlPointId.None) {
								ICommand cmd = new ConnectCommand(selectedShape, gluePointId, shapeInfo.Shape, shapeInfo.ControlPointId);
								diagramPresenter.Project.ExecuteCommand(cmd);
							}
							//else if (shape.ContainsPoint(gluePointPos.X, gluePointPos.Y)) {
							//   ICommand cmd = new ConnectCommand(selectedShape, gluePointId, shape, ControlPointId.Reference);
							//   display.Project.ExecuteCommand(cmd);
							//}
						}
					}
				}
			}
		}

		#endregion


		#region Moving Shapes

		// prepare drawing preview of move action
		private bool PrepareMoveShapePreview(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			Assert(diagramPresenter.SelectedShapes.Count > 0);
			Assert(!SelectedShapeAtCursorInfo.IsEmpty);
			// calculate the movement
			int distanceX = mouseState.X - ActionStartMouseState.X;
			int distanceY = mouseState.Y - ActionStartMouseState.Y;
			// calculate "Snap to Grid" offset
			snapDeltaX = snapDeltaY = 0;
			if (diagramPresenter.SnapToGrid) {
				FindNearestSnapPoint(diagramPresenter, SelectedShapeAtCursorInfo.Shape, distanceX, distanceY, out snapDeltaX, out snapDeltaY);
				distanceX += snapDeltaX;
				distanceY += snapDeltaY;
			}
			// move selectedShapes
			Rectangle shapeBounds = Rectangle.Empty;
			foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
				Shape preview = FindPreviewOfShape(selectedShape);
				preview.MoveTo(selectedShape.X + distanceX, selectedShape.Y + distanceY);
			}
			return true;
		}


		// apply the move action
		private bool PerformMoveShape(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;
			if (SelectedShapeAtCursorInfo.IsEmpty) {
				// Das SOLLTE nie passieren - passiert aber leider ab und zu... :-(
				return result;
			}

			if (ActionStartMouseState.Position != mouseState.Position) {
				// calculate the movement
				int distanceX = mouseState.X - ActionStartMouseState.X;
				int distanceY = mouseState.Y - ActionStartMouseState.Y;
				snapDeltaX = snapDeltaY = 0;
				if (diagramPresenter.SnapToGrid)
					FindNearestSnapPoint(diagramPresenter, SelectedShapeAtCursorInfo.Shape, distanceX, distanceY, out snapDeltaX, out snapDeltaY, ControlPointCapabilities.All);

				ICommand cmd = new MoveShapeByCommand(diagramPresenter.SelectedShapes, distanceX + snapDeltaX, distanceY + snapDeltaY);
				diagramPresenter.Project.ExecuteCommand(cmd);

				snapDeltaX = snapDeltaY = 0;
				snapPtId = ControlPointId.None;
				result = true;
			}
			return result;
		}

		#endregion


		#region Moving ControlPoints

		// prepare drawing preview of resize action 
		private bool PrepareMoveHandlePreview(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			int distanceX = mouseState.X - ActionStartMouseState.X;
			int distanceY = mouseState.Y - ActionStartMouseState.Y;

			// calculate "Snap to Grid/ControlPoint" offset
			snapDeltaX = snapDeltaY = 0;
			if (SelectedShapeAtCursorInfo.IsCursorAtGluePoint) {
				ControlPointId targetPtId;
				Shape targetShape = FindNearestControlPoint(diagramPresenter, SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Connect, distanceX, distanceY, out snapDeltaX, out snapDeltaY, out targetPtId);
			} else
				FindNearestSnapPoint(diagramPresenter, SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, distanceX, distanceY, out snapDeltaX, out snapDeltaY);
			distanceX += snapDeltaX;
			distanceY += snapDeltaY;

			// ToDo: optimize this: fewer move operations
			// (This code does not work yet)
			//Point originalPtPos = Point.Empty;
			//for (int i = 0; i < display.SelectedShapes.Count; ++i) {
			//   // reset position
			//   originalPtPos = display.SelectedShapes[i].GetControlPointPosition(SelectedPointId);
			//   // perform new movement
			//   if (Previews[i].HasControlPointCapability(SelectedPointId, ControlPointCapabilities.Resize))
			//      Previews[i].MoveControlPointTo(SelectedPointId, originalPtPos.X + distanceX, originalPtPos.Y + distanceY, CurrentModifiers);
			//}

			// move selected shapes
			Point originalPtPos = Point.Empty;
			foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
				Shape previewShape = FindPreviewOfShape(selectedShape);

				// reset position
				originalPtPos = selectedShape.GetControlPointPosition(SelectedShapeAtCursorInfo.ControlPointId);
				// ToDo: Restore ResizeModifiers
				previewShape.MoveControlPointTo(SelectedShapeAtCursorInfo.ControlPointId, originalPtPos.X, originalPtPos.Y, ResizeModifiers.None);
				previewShape.MoveControlPointTo(ControlPointId.Reference, selectedShape.X, selectedShape.Y, ResizeModifiers.None);

				// perform new movement
				if (previewShape.HasControlPointCapability(SelectedShapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Resize))
					// ToDo: Restore ResizeModifiers
					previewShape.MoveControlPointBy(SelectedShapeAtCursorInfo.ControlPointId, distanceX, distanceY, ResizeModifiers.None);
			}
			return true;
		}


		// apply the resize action
		private bool PerformMoveHandle(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;
			Invalidate(diagramPresenter);

			int distanceX = mouseState.X - ActionStartMouseState.X;
			int distanceY = mouseState.Y - ActionStartMouseState.Y;

			// if the moved ControlPoint is a single GluePoint, snap to ConnectionPoints
			snapDeltaX = snapDeltaY = 0;
			bool isGluePoint = false;
			if (diagramPresenter.SelectedShapes.Count == 1)
				isGluePoint = SelectedShapeAtCursorInfo.Shape.HasControlPointCapability(SelectedShapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Glue);

			// Snap to Grid or ControlPoint
			bool calcSnapDistance = true;
			ShapeAtCursorInfo targetShapeInfo = ShapeAtCursorInfo.Empty;
			if (isGluePoint) {
				Point currentPtPos = SelectedShapeAtCursorInfo.Shape.GetControlPointPosition(SelectedShapeAtCursorInfo.ControlPointId);
				Point newPtPos = Point.Empty;
				newPtPos.Offset(currentPtPos.X + distanceX, currentPtPos.Y + distanceY);
				targetShapeInfo = FindConnectionTarget(ActionDiagramPresenter, SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, newPtPos, true);
				if (!targetShapeInfo.IsEmpty) {
					calcSnapDistance = false;
					if (targetShapeInfo.ControlPointId != ControlPointId.Reference) {
						distanceX = newPtPos.X - currentPtPos.X;
						distanceY = newPtPos.Y - currentPtPos.Y;
					}
				}
			}
			if (calcSnapDistance) {
				FindNearestSnapPoint(diagramPresenter, SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, distanceX, distanceY, out snapDeltaX, out snapDeltaY);
				distanceX += snapDeltaX;
				distanceY += snapDeltaY;
			}

			if (isGluePoint) {
				ICommand cmd = new MoveGluePointCommand(SelectedShapeAtCursorInfo.Shape, SelectedShapeAtCursorInfo.ControlPointId, targetShapeInfo.Shape, distanceX, distanceY, ResizeModifiers.None);
				diagramPresenter.Project.ExecuteCommand(cmd);
			} else {
				// ToDo: Re-activate ResizeModifiers
				ICommand cmd = new MoveControlPointCommand(ActionDiagramPresenter.SelectedShapes, SelectedShapeAtCursorInfo.ControlPointId, distanceX, distanceY, ResizeModifiers.None);
				diagramPresenter.Project.ExecuteCommand(cmd);
			}

			snapDeltaX = snapDeltaY = 0;
			snapPtId = ControlPointId.None;
			result = true;

			return result;
		}

		#endregion


		#region Rotating Shapes

		private int CalcStartAngle(MouseState startMouseState, MouseState currentMouseState) {
			Assert(startMouseState != MouseState.Empty);
			Assert(currentMouseState != MouseState.Empty);
			float angleRad = Geometry.Angle(startMouseState.Position, currentMouseState.Position);
			return (3600 + Geometry.RadiansToTenthsOfDegree(angleRad)) % 3600;
		}


		private int CalcSweepAngle(MouseState initMouseState, MouseState prevMouseState, MouseState newMouseState) {
			Assert(initMouseState != MouseState.Empty);
			Assert(prevMouseState != MouseState.Empty);
			Assert(newMouseState != MouseState.Empty);
			float angleRad = Geometry.Angle(initMouseState.Position, prevMouseState.Position, newMouseState.Position);
			return (3600 + Geometry.RadiansToTenthsOfDegree(angleRad)) % 3600;
		}


		private int AlignAngle(int angle, MouseState mouseState) {
			int result = angle;
			if (mouseState.IsKeyPressed(KeysDg.Control) && mouseState.IsKeyPressed(KeysDg.Shift)) {
				// rotate by tenths of degrees
				// do nothing 
			} else if (mouseState.IsKeyPressed(KeysDg.Control)) {
				// rotate by full degrees
				result -= (result % 10);
			} else if (mouseState.IsKeyPressed(KeysDg.Shift)) {
				// rotate by 5 degrees
				result -= (result % 50);
			} else {
				// default:
				// rotate by 15 degrees
				result -= (result % 150);
			}
			return result;
		}


		private void CalcAngle(MouseState initMouseState, MouseState startMouseState, MouseState newMouseState, out int startAngle, out int sweepAngle) {
			startAngle = CalcStartAngle(initMouseState, ActionStartMouseState);
			int rawSweepAngle = CalcSweepAngle(initMouseState, ActionStartMouseState, newMouseState);
			sweepAngle = AlignAngle(rawSweepAngle, newMouseState);
		}


		private void CalcAngle(MouseState initMouseState, MouseState startMouseState, MouseState currentMouseState, MouseState newMouseState, out int startAngle, out int sweepAngle, out int prevSweepAngle) {
			CalcAngle(initMouseState, startMouseState, newMouseState, out startAngle, out sweepAngle);
			int rawPrevSweepAngle = CalcSweepAngle(initMouseState, ActionStartMouseState, currentMouseState);
			prevSweepAngle = AlignAngle(rawPrevSweepAngle, currentMouseState);
		}


		private bool IsMinRotateRangeExceeded(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (mouseState == MouseState.Empty) throw new ArgumentException("mouseState");
			Point p;
			if (PendingToolActionsCount <= 1) p = ActionStartMouseState.Position;
			else {
				MouseState prevMouseState = GetPreviousMouseState();
				p = prevMouseState.Position;
			}
			Debug.Assert(Geometry.IsValid(p));
			int dist = (int)Math.Round(Geometry.DistancePointPoint(p.X, p.Y, mouseState.X, mouseState.Y));
			diagramPresenter.DiagramToControl(dist, out dist);
			return (dist > diagramPresenter.MinRotateRange);
		}


		private MouseState GetPreviousMouseState() {
			MouseState result = MouseState.Empty;
			bool firstItem = true;
			foreach (ActionDef actionDef in PendingToolActions) {
				if (!firstItem) {
					result = actionDef.MouseState;
					break;
				} else firstItem = false;
			}
			return result;
		}



		// prepare drawing preview of rotate action
		private bool PrepareRotatePreview(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			if (PendingToolActionsCount >= 1
				&& ActionStartMouseState.Position != mouseState.Position) {
				if (IsMinRotateRangeExceeded(diagramPresenter, mouseState)) {
					// calculate new angle
					MouseState initMouseState = GetPreviousMouseState();
					int startAngle, sweepAngle, prevSweepAngle;
					CalcAngle(initMouseState, ActionStartMouseState, CurrentMouseState, mouseState,
						out startAngle, out sweepAngle, out prevSweepAngle);

					// ToDo: Implement rotation around a common rotation center
					Point rotationCenter = Point.Empty;
					foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
						Shape previewShape = FindPreviewOfShape(selectedShape);
						// Get ControlPointId of the first rotate control point
						ControlPointId rotatePtId = ControlPointId.None;
						foreach (ControlPointId id in previewShape.GetControlPointIds(ControlPointCapabilities.Rotate)) {
							rotatePtId = id;
							break;
						}
						if (rotatePtId == ControlPointId.None) throw new NShapeInternalException("{0} has no rotate control point.");
						rotationCenter = previewShape.GetControlPointPosition(rotatePtId);

						// Restore original shape's angle
						previewShape.Rotate(-prevSweepAngle, rotationCenter.X, rotationCenter.Y);
						// Perform rotation
						previewShape.Rotate(sweepAngle, rotationCenter.X, rotationCenter.Y);
					}
				}
			}
			return true;
		}


		// apply rotate action
		private bool PerformRotate(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;
			if (PendingToolActionsCount >= 1
				&& ActionStartMouseState.Position != mouseState.Position
				&& IsMinRotateRangeExceeded(ActionDiagramPresenter, mouseState)) {
				// Calculate rotation
				MouseState initMouseState = GetPreviousMouseState();
				int startAngle, sweepAngle;
				CalcAngle(initMouseState, ActionStartMouseState, mouseState, out startAngle, out sweepAngle);
				// Create and execute command
				ICommand cmd = new RotateShapesCommand(diagramPresenter.SelectedShapes, sweepAngle);
				diagramPresenter.Project.ExecuteCommand(cmd);
				result = true;
			}
			return result;
		}


		/// <summary>
		/// Specifies if a double click on the rotation handle will rotate the shape by 90�
		/// </summary>
		public bool EnableQuickRotate {
			get { return enableQuickRotate; }
			set { enableQuickRotate = value; }
		}


		private bool PerformQuickRotate(IDiagramPresenter diagramPresenter, int angle) {
			bool result = false;
			if (enableQuickRotate) {
				ICommand cmd = new RotateShapesCommand(diagramPresenter.SelectedShapes, angle);
				diagramPresenter.Project.ExecuteCommand(cmd);
				InvalidateAnglePreview(diagramPresenter);
				result = true;
			}
			return result;
		}


		private void InvalidateAnglePreview(IDiagramPresenter diagramPresenter) {
			// invalidate previous shapeAngle preview
			diagramPresenter.InvalidateDiagram(
				rectBuffer.X - rectBuffer.Width - diagramPresenter.GripSize,
				rectBuffer.Y - rectBuffer.Height - diagramPresenter.GripSize,
				rectBuffer.Width + rectBuffer.Width + (2 * diagramPresenter.GripSize),
				rectBuffer.Height + rectBuffer.Height + (2 * diagramPresenter.GripSize));

			int requiredDistance;
			diagramPresenter.ControlToDiagram(diagramPresenter.MinRotateRange, out requiredDistance);
			int length = (int)Math.Round(Geometry.DistancePointPoint(ActionStartMouseState.X, ActionStartMouseState.Y, CurrentMouseState.X, CurrentMouseState.Y));

			// invalidate current angle preview / instruction preview
			rectBuffer.Location = ActionStartMouseState.Position;
			if (length > requiredDistance)
				rectBuffer.Width = rectBuffer.Height = length;
			else
				rectBuffer.Width = rectBuffer.Height = requiredDistance;
			diagramPresenter.InvalidateDiagram(rectBuffer.X - rectBuffer.Width, rectBuffer.Y - rectBuffer.Height, rectBuffer.Width + rectBuffer.Width, rectBuffer.Height + rectBuffer.Height);
		}

		#endregion


		#region Title Editor

		//private void ShowTextEditor(string pressedKey) {
		//   // show TextEditor
		//   if (CurrentDisplay.SelectedShapes.Count == 1) {
		//      if (CurrentDisplay.SelectedShapes.TopMost is ICaptionedShape) {
		//         ICaptionedShape labeledShape = (ICaptionedShape)CurrentDisplay.SelectedShapes.TopMost;
		//         if (labeledShape.CaptionCount > 0) CurrentDisplay.OpenCaptionEditor(labeledShape, 0, pressedKey);
		//      }
		//   }
		//}

		#endregion

		#endregion


		#region [Private] Preview management implementation

		/// <summary>
		/// The dictionary of preview shapes: The key is the original shape, the value is the preview shape.
		/// </summary>
		private IDictionary<Shape, Shape> Previews {
			get { return previewShapes; }
		}


		private Shape FindPreviewOfShape(Shape shape) {
			if (shape == null) throw new ArgumentNullException("shape");
			Assert(previewShapes.ContainsKey(shape), string.Format("No preview found for '{0}' shape.", shape.Type.Name));
			return previewShapes[shape];
		}


		private Shape FindShapeOfPreview(Shape previewShape) {
			if (previewShape == null) throw new ArgumentNullException("previewShape");
			Assert(originalShapes.ContainsKey(previewShape), string.Format("No original shape found for '{0}' preview shape.", previewShape.Type.Name));
			return originalShapes[previewShape];
		}


		private void AddPreview(Shape shape, Shape previewShape, IDisplayService displayService) {
			if (originalShapes.ContainsKey(previewShape)) return;
			if (previewShapes.ContainsKey(shape)) return;
			// Set DisplayService for the preview shape
			if (previewShape.DisplayService != displayService)
				previewShape.DisplayService = displayService;

			// Add shape and its preview to the appropriate dictionaries
			previewShapes.Add(shape, previewShape);
			originalShapes.Add(previewShape, shape);

			// Add shape's children and their previews to the appropriate dictionaries
			if (previewShape.Children.Count > 0) {
				IEnumerator<Shape> previewChildren = previewShape.Children.TopDown.GetEnumerator();
				IEnumerator<Shape> originalChildren = shape.Children.TopDown.GetEnumerator();

				previewChildren.Reset();
				originalChildren.Reset();
				bool processNext = false;
				if (previewChildren.MoveNext() && originalChildren.MoveNext())
					processNext = true;
				while (processNext) {
					AddPreview(originalChildren.Current, previewChildren.Current, displayService);
					processNext = (previewChildren.MoveNext() && originalChildren.MoveNext());
				}
			}
		}


		private void RemovePreviewOf(Shape originalShape) {
			if (previewShapes.ContainsKey(originalShape)) {
				// Invalidate Preview Shape
				Shape previewShape = Previews[originalShape];
				previewShape.Invalidate();

				// remove previews of the shape and its children from the preview's dictionary
				previewShapes.Remove(originalShape);
				if (originalShape.Children.Count > 0) {
					foreach (Shape childShape in originalShape.Children)
						previewShapes.Remove(childShape);
				}
				// remove the shape and its children from the shape's dictionary
				originalShapes.Remove(previewShape);
				if (previewShape.Children.Count > 0) {
					foreach (Shape childShape in previewShape.Children)
						originalShapes.Remove(childShape);
				}
			}
		}


		private void RemovePreview(Shape previewShape) {
			Shape origShape = null;
			if (!originalShapes.TryGetValue(previewShape, out origShape))
				throw new NShapeInternalException("This preview shape has no associated original shape in this tool.");
			else {
				// Invalidate Preview Shape
				previewShape.Invalidate();
				// Remove both, original- and preview shape from the appropriate dictionaries
				previewShapes.Remove(origShape);
				originalShapes.Remove(previewShape);
			}
		}


		private void ClearPreviews() {
			foreach (KeyValuePair<Shape, Shape> item in previewShapes) {
				Shape preview = item.Value;
				preview.Invalidate();
				preview.DisplayService = null;
				preview.Dispose();
			}
			previewShapes.Clear();
			originalShapes.Clear();
		}


		private bool IsConnectedToNonSelectedShape(IDiagramPresenter diagramPresenter, Shape shape) {
			foreach (ControlPointId gluePointId in shape.GetControlPointIds(ControlPointCapabilities.Glue)) {
				ShapeConnectionInfo sci = shape.GetConnectionInfo(gluePointId, null);
				if (!sci.IsEmpty
					&& !diagramPresenter.SelectedShapes.Contains(sci.OtherShape))
					return true;
			}
			return false;
		}


		/// <summary>
		/// Create previews of shapes connected to the given shape (and it's children) and connect them to the
		/// shape's preview (or the preview of it's child)
		/// </summary>
		/// <param name="shape">The original shape which contains all ConnectionInfo</param>
		private void ConnectPreviewOfShape(IDiagramPresenter diagramPresenter, Shape shape) {
			// process shape's children
			if (shape.Children != null && shape.Children.Count > 0) {
				foreach (Shape childShape in shape.Children)
					ConnectPreviewOfShape(diagramPresenter, childShape);
			}

			Shape preview = FindPreviewOfShape(shape);
			foreach (ShapeConnectionInfo connectionInfo in shape.GetConnectionInfos(ControlPointId.Any, null)) {
				if (diagramPresenter.SelectedShapes.Contains(connectionInfo.OtherShape)) {
					// Do not connect previews if BOTH of the connected shapes are part of the selection because 
					// this would restrict movement of the connector shapes and decreases performance (many 
					// unnecessary FollowConnectionPointWithGluePoint() calls)
					if (shape.HasControlPointCapability(connectionInfo.OwnPointId, ControlPointCapabilities.Glue)) {
						if (IsConnectedToNonSelectedShape(diagramPresenter, shape)) {
							Shape targetPreview = FindPreviewOfShape(connectionInfo.OtherShape);
							preview.Connect(connectionInfo.OwnPointId, targetPreview, connectionInfo.OtherPointId);
						}
					}
				} else {
					// Connect preview of shape to a non-selected shape if it is a single shape 
					// that has a glue point (e.g. a Label)
					if (preview.HasControlPointCapability(connectionInfo.OwnPointId, ControlPointCapabilities.Glue)) {
						// Only connect if the control point to be connected is not the control point to be moved
						if (shape == SelectedShapeAtCursorInfo.Shape && connectionInfo.OwnPointId != SelectedShapeAtCursorInfo.ControlPointId)
							preview.Connect(connectionInfo.OwnPointId, connectionInfo.OtherShape, connectionInfo.OtherPointId);
					} else
						// Create a preview of the shape that is connected to the preview (recursive call)
						CreateConnectedTargetPreviewShape(diagramPresenter, preview, connectionInfo);
				}
			}
		}


		/// <summary>
		/// Creates (or finds) a preview of the connection's PassiveShape and connects it to the current preview shape
		/// </summary>
		/// <param name="previewShape">The preview shape</param>
		/// <param name="connectionInfo">ConnectionInfo of the original (non-preview) shape</param>
		private void CreateConnectedTargetPreviewShape(IDiagramPresenter diagramPresenter, Shape previewShape, ShapeConnectionInfo connectionInfo) {
			// Check if any other selected shape is connected to the same non-selected shape
			Shape previewTargetShape;
			// If the current passiveShape is already connected to another shape of the current selection,
			// connect the current preview to the other preview's passiveShape
			if (!targetShapeBuffer.TryGetValue(connectionInfo.OtherShape, out previewTargetShape)) {
				// If the current passiveShape is not connected to any other of the selected selectedShapes,
				// create a clone of the passiveShape and connect it to the corresponding preview
				// If the preview exists, abort connecting (in this case, the shape is a preview of a child shape)
				if (previewShapes.ContainsKey(connectionInfo.OtherShape)) return;
				else {
					previewTargetShape = connectionInfo.OtherShape.Type.CreatePreviewInstance(connectionInfo.OtherShape);
					AddPreview(connectionInfo.OtherShape, previewTargetShape, diagramPresenter.DisplayService);
				}
				// Add passive shape and it's clone to the passive shape dictionary
				targetShapeBuffer.Add(connectionInfo.OtherShape, previewTargetShape);
			}
			// Connect the (new or existing) preview shapes
			// Skip connecting if the preview is already connected.
			Assert(previewTargetShape != null, "Error while creating connected preview shapes.");
			if (previewTargetShape.IsConnected(connectionInfo.OtherPointId, null) == ControlPointId.None) {
				previewTargetShape.Connect(connectionInfo.OtherPointId, previewShape, connectionInfo.OwnPointId);
				// check, if any shapes are connected to the connector (that is connected to the selected shape)
				foreach (ShapeConnectionInfo connectorCI in connectionInfo.OtherShape.GetConnectionInfos(ControlPointId.Any, null)) {
					// skip if the connector is connected to the shape with more than one glue point
					if (connectorCI.OtherShape == FindShapeOfPreview(previewShape)) continue;
					if (connectorCI.OwnPointId != connectionInfo.OtherPointId) {
						// Check if the shape on the other end is selected.
						// If it is, connect to it's preview or skip connecting if the target preview does 
						// not exist yet (it will be connected when creating the targt's preview)
						if (diagramPresenter.SelectedShapes.Contains(connectorCI.OtherShape)) {
							if (previewShapes.ContainsKey(connectorCI.OtherShape)) {
								Shape s = FindPreviewOfShape(connectorCI.OtherShape);
								if (s.IsConnected(connectorCI.OtherPointId, previewTargetShape) == ControlPointId.None)
									previewTargetShape.Connect(connectorCI.OwnPointId, s, connectorCI.OtherPointId);
							} else continue;
						} else if (connectorCI.OtherShape.HasControlPointCapability(connectorCI.OtherPointId, ControlPointCapabilities.Glue))
							// Connect connectors connected to the previewTargetShape
							CreateConnectedTargetPreviewShape(diagramPresenter, previewTargetShape, connectorCI);
						else if (connectorCI.OtherPointId == ControlPointId.Reference) {
							// Connect the other end of the previewTargetShape if the connection is a Point-To-Shape connection
							Assert(connectorCI.OtherShape.IsConnected(connectorCI.OtherPointId, previewTargetShape) == ControlPointId.None);
							Assert(previewTargetShape.IsConnected(connectorCI.OwnPointId, null) == ControlPointId.None);
							previewTargetShape.Connect(connectorCI.OwnPointId, connectorCI.OtherShape, connectorCI.OtherPointId);
						}
					}
				}
			}
		}


		#endregion


		#region [Private] Helper Methods

		private void SetSelectedShapeAtCursor(IDiagramPresenter diagramPresenter, int mouseX, int mouseY, int handleRadius, ControlPointCapabilities handleCapabilities) {
			// Find the shape under the cursor
			selShapeAtCursorInfo.Clear();
			selShapeAtCursorInfo.Shape = diagramPresenter.SelectedShapes.FindShape(mouseX, mouseY, handleCapabilities, handleRadius, null);

			// If there is a shape under the cursor, find the nearest control point and caption
			if (!selShapeAtCursorInfo.IsEmpty) {
				// Find control point at cursor that belongs to the selected shape at cursor
				selShapeAtCursorInfo.ControlPointId = selShapeAtCursorInfo.Shape.FindNearestControlPoint(mouseX, mouseY, diagramPresenter.ZoomedGripSize, gripCapabilities);
				// Find caption at cursor (if the shape is a captioned shape)
				if (selShapeAtCursorInfo.Shape is ICaptionedShape && ((ICaptionedShape)selShapeAtCursorInfo.Shape).CaptionCount > 0)
					selShapeAtCursorInfo.CaptionIndex = ((ICaptionedShape)selShapeAtCursorInfo.Shape).FindCaptionFromPoint(mouseX, mouseY);
			}
		}


		private bool ShapeOrShapeRelativesContainsPoint(Shape shape, int x, int y, ControlPointCapabilities capabilities, int range) {
			if (shape.HitTest(x, y, capabilities, range) != ControlPointId.None)
				return true;
			else if (shape.Parent != null) {
				if (ShapeOrShapeRelativesContainsPoint(shape.Parent, x, y, capabilities, range))
					return true;
			}
			return false;
		}


		private int DetermineCursor(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			switch (CurrentAction) {
				case Action.None:
					// If no action is pending, the folowing cursors are possible:
					// - Default (no selected shape under cursor or action not granted)
					// - Move shape cursor
					// - Move grip cursor
					// - Rotate cursor
					// - Edit caption cursor
					if (!SelectedShapeAtCursorInfo.IsEmpty) {
						// Check if cursor is over a caption and editing caption is feasible
						if (IsEditCaptionFeasible(diagramPresenter, mouseState, SelectedShapeAtCursorInfo))
							return cursors[ToolCursor.EditCaption];
						// Check if cursor is over a control point and moving grips or rotating is feasible
						if (SelectedShapeAtCursorInfo.IsCursorAtGrip) {
							if (IsMoveHandleFeasible(diagramPresenter, SelectedShapeAtCursorInfo))
								return cursors[ToolCursor.MoveHandle];
							else if (IsRotatatingFeasible(diagramPresenter, SelectedShapeAtCursorInfo))
								return cursors[ToolCursor.Rotate];
							else return cursors[ToolCursor.Default];
						}
						// Check if cursor is inside the shape and move shape is feasible
						if (IsMoveShapeFeasible(diagramPresenter, mouseState, SelectedShapeAtCursorInfo))
							return cursors[ToolCursor.MoveShape];
					}
					return cursors[ToolCursor.Default];

				case Action.Select:
				case Action.SelectWithFrame:
					return cursors[ToolCursor.Default];

				case Action.EditCaption:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					Assert(SelectedShapeAtCursorInfo.Shape is ICaptionedShape);
					// If the cursor is outside the caption, return default cursor
					int captionIndex = ((ICaptionedShape)SelectedShapeAtCursorInfo.Shape).FindCaptionFromPoint(mouseState.X, mouseState.Y);
					if (captionIndex == SelectedShapeAtCursorInfo.CaptionIndex)
						return cursors[ToolCursor.EditCaption];
					else return cursors[ToolCursor.Default];

				case Action.MoveHandle:
					Assert(!SelectedShapeAtCursorInfo.IsEmpty);
					Assert(SelectedShapeAtCursorInfo.IsCursorAtGrip);
					if (SelectedShapeAtCursorInfo.IsCursorAtGluePoint) {
						Shape previewShape = FindPreviewOfShape(SelectedShapeAtCursorInfo.Shape);
						Point ptPos = previewShape.GetControlPointPosition(SelectedShapeAtCursorInfo.ControlPointId);
						ShapeAtCursorInfo shapeAtCursorInfo = FindConnectionTarget(
							diagramPresenter,
							SelectedShapeAtCursorInfo.Shape,
							SelectedShapeAtCursorInfo.ControlPointId,
							ptPos, 
							true);
						if (!shapeAtCursorInfo.IsEmpty && shapeAtCursorInfo.IsCursorAtGrip)
							return cursors[ToolCursor.Connect];
					}
					return cursors[ToolCursor.MoveHandle];

				case Action.MoveShape:
					return cursors[ToolCursor.MoveShape];

				case Action.PrepareRotate:
				case Action.Rotate:
					return cursors[ToolCursor.Rotate];

				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}
		}


		/// <summary>
		/// Create Previews of all shapes selected in the CurrentDisplay.
		/// These previews are connected to all the shapes the original shapes are connected to.
		/// </summary>
		private void CreatePreviewShapes(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (Previews.Count == 0 && diagramPresenter.SelectedShapes.Count > 0) {
				// first, clone all selected shapes...
				foreach (Shape shape in diagramPresenter.SelectedShapes)
					AddPreview(shape, shape.Type.CreatePreviewInstance(shape), diagramPresenter.DisplayService);
				// ...then restore connections between previews and connections between previews and non-selected shapes
				targetShapeBuffer.Clear();
				foreach (Shape selectedShape in diagramPresenter.SelectedShapes.BottomUp) {
					// AttachGluePointToConnectionPoint the preview shape (and all it's cildren) to all the shapes the original shape was connected to
					// Additionally, create previews for all connected shapes and connect these to the appropriate target shapes
					ConnectPreviewOfShape(diagramPresenter, selectedShape);
				}
				targetShapeBuffer.Clear();
			}
		}


		private void ResetPreviewShapes(IDiagramPresenter diagramPresenter) {
			foreach (KeyValuePair<Shape, Shape> item in previewShapes)
				item.Value.Dispose();
			previewShapes.Clear();
			CreatePreviewShapes(diagramPresenter);
		}


		private void InvalidateShapes(IDiagramPresenter diagramPresenter, IEnumerable<Shape> shapes) {
			foreach (Shape shape in shapes)
				DoInvalidateShape(diagramPresenter, shape);
		}


		private void DoInvalidateShape(IDiagramPresenter diagramPresenter, Shape shape) {
			if (shape.Parent != null)
				DoInvalidateShape(diagramPresenter, shape.Parent);
			else {
				shape.Invalidate();
				diagramPresenter.InvalidateGrips(shape, ControlPointCapabilities.All);
			}
		}


		private bool IsMoveShapeFeasible(IDiagramPresenter diagramPresenter, MouseState mouseState, ShapeAtCursorInfo shapeAtCursorInfo) {
			if (shapeAtCursorInfo.IsEmpty)
				return false;
			if (!diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout, shapeAtCursorInfo.Shape))
				return false;
			if (diagramPresenter.SelectedShapes.Count > 0 && !diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout, diagramPresenter.SelectedShapes))
				return false;
			if (!shapeAtCursorInfo.Shape.ContainsPoint(mouseState.X, mouseState.Y))
				return false;

			if (diagramPresenter.SelectedShapes.Contains(shapeAtCursorInfo.Shape)) {
				// ToDo: If there are *many* shapes selected (e.g. 10000), this check will be extremly slow...
				if (diagramPresenter.SelectedShapes.Count < 10000) {
					// LinearShapes that own connected gluePoints may not be moved.
					foreach (Shape shape in diagramPresenter.SelectedShapes) {
						if (shape is ILinearShape) {
							foreach (ControlPointId gluePointId in shape.GetControlPointIds(ControlPointCapabilities.Glue)) {
								ShapeConnectionInfo sci = shape.GetConnectionInfo(gluePointId, null);
								if (!sci.IsEmpty) {
									// Allow movement if the connected shapes are moved together
									if (!diagramPresenter.SelectedShapes.Contains(sci.OtherShape))
										return false;
								}
							}
						}
					}
				}
			}
			return true;
		}


		private bool IsMoveHandleFeasible(IDiagramPresenter diagramPresenter, ShapeAtCursorInfo shapeAtCursorInfo) {
			if (shapeAtCursorInfo.IsEmpty)
				return false;
			if (!diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout, diagramPresenter.SelectedShapes))
				return false;
			if (!shapeAtCursorInfo.Shape.HasControlPointCapability(shapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Resize | ControlPointCapabilities.Glue))
				return false;
			if (diagramPresenter.SelectedShapes.Count > 1) {
				// GluePoints may only be moved alone
				if (shapeAtCursorInfo.Shape.HasControlPointCapability(shapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Glue))
					return false;
				// Check if all shapes that are going to be resizes are of the same type
				Shape lastShape = null;
				foreach (Shape shape in diagramPresenter.SelectedShapes) {
					if (lastShape != null && lastShape.Type != shape.Type)
						return false;
					lastShape = shape;
				}
			}
			return true;
		}


		private bool IsRotatatingFeasible(IDiagramPresenter diagramPresenter, ShapeAtCursorInfo shapeAtCursorInfo) {
			if (shapeAtCursorInfo.IsEmpty)
				return false;
			if (!diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout, diagramPresenter.SelectedShapes))
				return false;
			if (!shapeAtCursorInfo.Shape.HasControlPointCapability(shapeAtCursorInfo.ControlPointId, ControlPointCapabilities.Rotate))
				return false;
			if (diagramPresenter.SelectedShapes.Count > 1) {
				// check if all selected shapes have a rotate handle
				foreach (Shape selectedShape in diagramPresenter.SelectedShapes) {
					bool shapeHasRotateHandle = false;
					foreach (ControlPointId ptId in selectedShape.GetControlPointIds(ControlPointCapabilities.Rotate)) {
						shapeHasRotateHandle = true;
						break;
					}
					if (!shapeHasRotateHandle) return false;
				}
			}
			return true;
		}


		private bool IsEditCaptionFeasible(IDiagramPresenter diagramPresenter, MouseState mouseState, ShapeAtCursorInfo shapeAtCursorInfo) {
			if (shapeAtCursorInfo.IsEmpty)
				return false;
			if (!diagramPresenter.Project.SecurityManager.IsGranted(Permission.ModifyData, shapeAtCursorInfo.Shape))
				return false;
			if (!shapeAtCursorInfo.IsCursorAtCaption)
				return false;
			if (mouseState.IsKeyPressed(KeysDg.Control) || mouseState.IsKeyPressed(KeysDg.Shift))
				return false;
			return true;
		}

		#endregion


		#region [Private] Construction

		static PointerTool() {
			cursors = new Dictionary<ToolCursor, int>(8);
			// Register cursors
			cursors.Add(ToolCursor.Default, CursorProvider.DefaultCursorID);
			cursors.Add(ToolCursor.ActionDenied, CursorProvider.RegisterCursor(Properties.Resources.ActionDeniedCursor));
			cursors.Add(ToolCursor.EditCaption, CursorProvider.RegisterCursor(Properties.Resources.EditTextCursor));
			cursors.Add(ToolCursor.MoveShape, CursorProvider.RegisterCursor(Properties.Resources.MoveShapeCursor));
			cursors.Add(ToolCursor.MoveHandle, CursorProvider.RegisterCursor(Properties.Resources.MovePointCursor));
			cursors.Add(ToolCursor.Rotate, CursorProvider.RegisterCursor(Properties.Resources.RotateCursor));
			// ToDo: Create better Connect/Disconnect cursors
			cursors.Add(ToolCursor.Connect, CursorProvider.RegisterCursor(Properties.Resources.HandCursor));
			cursors.Add(ToolCursor.Disconnect, CursorProvider.RegisterCursor(Properties.Resources.HandCursor));
		}


		private void Construct() {
			Title = "Pointer";
			ToolTipText = "Select one or more objects by holding shift while clicking or drawing a frame."
				+ Environment.NewLine
				+ "Selected objects can be moved by dragging them to the target position or resized by dragging "
				+ "a control point to the target position.";

			SmallIcon = global::Dataweb.NShape.Properties.Resources.PointerIconSmall;
			SmallIcon.MakeTransparent(Color.Fuchsia);

			LargeIcon = global::Dataweb.NShape.Properties.Resources.PointerIconLarge;
			LargeIcon.MakeTransparent(Color.Fuchsia);

			frameRect = Rectangle.Empty;
		}

		#endregion


		#region [Private] Types

		private enum Action { None, Select, SelectWithFrame, EditCaption, MoveShape, MoveHandle, PrepareRotate, Rotate }


		private enum ToolCursor {
			Default,
			Rotate,
			MoveHandle,
			MoveShape,
			ActionDenied,
			EditCaption,
			Connect,
			Disconnect
		}


		// connection handling stuff
		private struct ConnectionInfoBuffer {

			public static readonly ConnectionInfoBuffer Empty;

			public static bool operator ==(ConnectionInfoBuffer x, ConnectionInfoBuffer y) { return (x.connectionInfo == y.connectionInfo && x.shape == y.shape); }

			public static bool operator !=(ConnectionInfoBuffer x, ConnectionInfoBuffer y) { return !(x == y); }

			public Shape shape;

			public ShapeConnectionInfo connectionInfo;

			public override bool Equals(object obj) { return obj is ConnectionInfoBuffer && this == (ConnectionInfoBuffer)obj; }

			public override int GetHashCode() { return base.GetHashCode(); }

			static ConnectionInfoBuffer() {
				Empty.shape = null;
				Empty.connectionInfo = ShapeConnectionInfo.Empty;
			}
		}

		#endregion


		#region Fields

		// --- Description of the tool ---
		private static Dictionary<ToolCursor, int> cursors;
		//
		private bool enableQuickRotate = false;
		private ControlPointCapabilities gripCapabilities = ControlPointCapabilities.Resize | ControlPointCapabilities.Rotate;

		// --- State after the last ProcessMouseEvent ---
		// selected shape under the mouse cursor, being highlighted in the next drawing
		private ShapeAtCursorInfo selShapeAtCursorInfo;
		// rectangle that represents the transformed selection area in control coordinates
		private Rectangle frameRect;
		// stores the distance the SelectedShape was moved on X-axis for snapping the nearest gridpoint
		private int snapDeltaX;
		// stores the distance the SelectedShape was moved on Y-axis for snapping the nearest gridpoint
		private int snapDeltaY;
		// index of the controlPoint that snapped to grid/point/swimline
		private int snapPtId;

		// -- Definition of current action
		// indicates the current action depending on the mouseButton State, selected selectedShapes and mouse movement
		//private ToolAction currentToolAction = ToolAction.None;
		// preview shapes (Key = original shape, Value = preview shape)
		private Dictionary<Shape, Shape> previewShapes = new Dictionary<Shape, Shape>();
		// original shapes (Key = preview shape, Value = original shape)
		private Dictionary<Shape, Shape> originalShapes = new Dictionary<Shape, Shape>();

		// Buffers
		// rectangle buffer 
		private Rectangle rectBuffer;
		// used for buffering selectedShapes connected to the preview selectedShapes: key = passiveShape, values = targetShapes's clone
		private Dictionary<Shape, Shape> targetShapeBuffer = new Dictionary<Shape, Shape>();
		// buffer used for storing connections that are temporarily disconnected for moving shapes
		private List<ConnectionInfoBuffer> connectionsBuffer = new List<ConnectionInfoBuffer>();

		#endregion
	}


	/// <summary>
	/// Lets the user create a templated shape.
	/// </summary>
	public abstract class TemplateTool : Tool {

		public Template Template {
			get { return template; }
		}


		/// <override></override>
		public override void Dispose() {
			// Do not dispose the Template - it has to be disposed by the cache
			base.Dispose();
		}


		/// <override></override>
		public override void RefreshIcons() {
			using (Shape clone = Template.Shape.Clone()) {
				clone.DrawThumbnail(base.LargeIcon, margin, transparentColor);
				base.LargeIcon.MakeTransparent(transparentColor);
			}
			using (Shape clone = Template.Shape.Clone()) {
				clone.DrawThumbnail(base.SmallIcon, margin, transparentColor);
				base.SmallIcon.MakeTransparent(transparentColor);
			}
			ClearPreview();
			Title = string.IsNullOrEmpty(Template.Title) ? Template.Name : Template.Title;
		}


		protected TemplateTool(Template template, string category)
			: base(category) {
			if (template == null) throw new ArgumentNullException("template");
			this.template = template;
			Title = string.IsNullOrEmpty(template.Title) ? template.Name : template.Title;
			ToolTipText = string.IsNullOrEmpty(template.Description) ? string.Format("Insert {0}", Title)
				: string.Format("Insert {0}: {1}", Title, template.Description);
			RefreshIcons();
		}


		protected TemplateTool(Template template)
			: this(template, (template != null) ? template.Shape.Type.DefaultCategoryTitle : null) {
		}


		protected Shape PreviewShape {
			get { return previewShape; }
		}


		protected virtual void CreatePreview(IDiagramPresenter diagramPresenter) {
			using (Shape s = Template.CreateShape())
				previewShape = Template.Shape.Type.CreatePreviewInstance(s);
			previewShape.DisplayService = diagramPresenter.DisplayService;
			previewShape.Invalidate();
		}


		protected virtual void ClearPreview() {
			if (previewShape != null) {
				previewShape.Invalidate();
				previewShape.Dispose();
				previewShape = null;
			}
		}


		#region Fields

		private Template template;
		private Shape previewShape;

		#endregion
	}


	/// <summary>
	/// Lets the user create a shape based on a point sequence.
	/// </summary>
	public class LinearShapeCreationTool : TemplateTool {

		public LinearShapeCreationTool(Template template)
			: this(template, null) {
		}


		public LinearShapeCreationTool(Template template, string category)
			: base(template, category) {
			if (!(template.Shape is ILinearShape))
				throw new NShapeException("The template's shape does not implement {0}.", typeof(ILinearShape).Name);
			if (template.Shape is PolylineBase)
				ToolTipText += Environment.NewLine + "Polylines are finished by double clicking.";
		}


		#region IDisposable Interface

		/// <override></override>
		public override void Dispose() {
			base.Dispose();
		}

		#endregion


		/// <override></override>
		public override IEnumerable<MenuItemDef> GetMenuItemDefs(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			yield break;
		}


		/// <override></override>
		public override bool ProcessMouseEvent(IDiagramPresenter diagramPresenter, MouseEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			bool result = false;

			MouseState newMouseState = MouseState.Empty;
			newMouseState.Buttons = e.Buttons;
			newMouseState.Modifiers = e.Modifiers;
			diagramPresenter.ControlToDiagram(e.Position, out newMouseState.Position);

			diagramPresenter.SuspendUpdate();
			try {
				switch (e.EventType) {
					case MouseEventType.MouseMove:
						if (CurrentMouseState.Position != newMouseState.Position)
							ProcessMouseMove(diagramPresenter, newMouseState);
						break;
					case MouseEventType.MouseDown:
						// MouseDown starts drag-based actions
						// ToDo: Implement these features: Adding Segments to existing Lines, Move existing Lines and their ControlPoints
						if (e.Clicks > 1) result = ProcessDoubleClick(diagramPresenter, newMouseState);
						else result = ProcessMouseClick(diagramPresenter, newMouseState);
						break;

					case MouseEventType.MouseUp:
						// MouseUp finishes drag-actions. Click-based actions are handled by the MouseClick event
						// ToDo: Implement these features: Adding Segments to existing Lines, Move existing Lines and their ControlPoints
						break;

					default: throw new NShapeUnsupportedValueException(e.EventType);
				}
				base.ProcessMouseEvent(diagramPresenter, e);
			} finally { diagramPresenter.ResumeUpdate(); }
			return result;
		}


		/// <override></override>
		public override bool ProcessKeyEvent(IDiagramPresenter diagramPresenter, KeyEventArgsDg e) {
			return base.ProcessKeyEvent(diagramPresenter, e);
		}


		/// <override></override>
		public override void EnterDisplay(IDiagramPresenter diagramPresenter) {
			Invalidate(diagramPresenter);
		}


		/// <override></override>
		public override void LeaveDisplay(IDiagramPresenter diagramPresenter) {
			Invalidate(diagramPresenter);
		}


		/// <override></override>
		public override void Invalidate(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (PreviewShape != null) {
				diagramPresenter.InvalidateGrips(PreviewShape, ControlPointCapabilities.All);
				Point p = PreviewShape.GetControlPointPosition(ControlPointId.LastVertex);
				InvalidateConnectionTargets(diagramPresenter, p.X, p.Y);
			} else InvalidateConnectionTargets(diagramPresenter, CurrentMouseState.X, CurrentMouseState.Y);
		}


		/// <override></override>
		public override void Draw(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			// Draw preview shape
			if (PreviewShape != null) {
				// Draw preview shape and its ControlPoints
				diagramPresenter.DrawShape(PreviewShape);
				diagramPresenter.ResetTransformation();
				try {
					foreach (ControlPointId pointId in PreviewShape.GetControlPointIds(ControlPointCapabilities.Glue | ControlPointCapabilities.Resize))
						diagramPresenter.DrawResizeGrip(IndicatorDrawMode.Normal, PreviewShape, pointId);
				} finally { diagramPresenter.RestoreTransformation(); }
			}

			// Highlight ConnectionPoints in range
			if (Template.Shape.HasControlPointCapability(ControlPointId.LastVertex, ControlPointCapabilities.Glue)) {
				if (PreviewShape == null) DrawConnectionTargets(diagramPresenter, CurrentMouseState.X, CurrentMouseState.Y);
				else {
					Point gluePtPos = PreviewShape.GetControlPointPosition(ControlPointId.LastVertex);
					DrawConnectionTargets(diagramPresenter, PreviewShape, ControlPointId.LastVertex, gluePtPos);
				}
			}
		}


		/// <override></override>
		protected override void StartToolAction(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantAutoScroll) {
			Debug.Print("StartToolAction");
			base.StartToolAction(diagramPresenter, action, mouseState, wantAutoScroll);
		}


		/// <override></override>
		protected override void EndToolAction() {
			Debug.Print("EndToolAction");
			base.EndToolAction();
			ClearPreview();
			lastInsertedPointId = ControlPointId.None;
			action = Action.None;
		}


		/// <override></override>
		protected override void CancelCore() {
			// Create the line until the last point that was created manually.
			// This feature only makes sense if an additional ControlPoint was created (other than the default points)
			ILinearShape templateShape = Template.Shape as ILinearShape;
			ILinearShape previewShape = PreviewShape as ILinearShape;
			if (IsToolActionPending && templateShape != null && previewShape != null 
				&& previewShape.VertexCount > templateShape.VertexCount)
					FinishLine(ActionDiagramPresenter, CurrentMouseState, true);
		}


		protected override void ClearPreview() {
			if (PreviewShape != null) {
				foreach (ControlPointId gluePtId in PreviewShape.GetControlPointIds(ControlPointCapabilities.Glue))
					PreviewShape.Disconnect(gluePtId);
				base.ClearPreview();
			}
		}


		static LinearShapeCreationTool() {
			cursors = new Dictionary<ToolCursor, int>(6);
			cursors.Add(ToolCursor.Default, CursorProvider.DefaultCursorID);
			cursors.Add(ToolCursor.Pen, CursorProvider.RegisterCursor(Properties.Resources.PenCursor));
			cursors.Add(ToolCursor.MovePoint, CursorProvider.RegisterCursor(Properties.Resources.MovePointCursor));
			cursors.Add(ToolCursor.Connect, CursorProvider.RegisterCursor(Properties.Resources.HandCursor));
			cursors.Add(ToolCursor.Disconnect, CursorProvider.RegisterCursor(Properties.Resources.HandCursor));
			cursors.Add(ToolCursor.NotAllowed, CursorProvider.RegisterCursor(Properties.Resources.ActionDeniedCursor));
			// ToDo: Create better cursors for connecting/disconnecting
		}


		private bool ProcessMouseMove(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			bool result = false;
			ShapeAtCursorInfo shapeAtCursorInfo = ShapeAtCursorInfo.Empty;

			// set cursor depending on the object under the mouse cursor
			int currentCursorId = DetermineCursor(diagramPresenter, shapeAtCursorInfo.Shape, shapeAtCursorInfo.ControlPointId);
			if (CurrentAction == Action.None)
				diagramPresenter.SetCursor(currentCursorId);
			else ActionDiagramPresenter.SetCursor(currentCursorId);

			switch (CurrentAction) {
				case Action.None:
					Invalidate(diagramPresenter);
					break;

				case Action.AddPoint:
					Invalidate(ActionDiagramPresenter);

					// ToDo: Replace ControlPointId.LastVertex with a Property for the Vertex under the cursor
					shapeAtCursorInfo = FindConnectionTarget(diagramPresenter, PreviewShape, ControlPointId.LastVertex, mouseState.Position, false);

					// check for connectionpoints wihtin the snapArea
					if (!shapeAtCursorInfo.IsEmpty) {
						Point p = Point.Empty;
						if (shapeAtCursorInfo.IsCursorAtGrip)
							p = shapeAtCursorInfo.Shape.GetControlPointPosition(shapeAtCursorInfo.ControlPointId);
						else p = mouseState.Position;
						// ToDo: Restore ResizeModifiers
						Assert(PreviewShape != null);
						if (PreviewShape != null)
							PreviewShape.MoveControlPointTo(ControlPointId.LastVertex, p.X, p.Y, ResizeModifiers.None);
					} else {
						int snapDeltaX = 0, snapDeltaY = 0;
						if (diagramPresenter.SnapToGrid)
							FindNearestSnapPoint(diagramPresenter, mouseState.X, mouseState.Y, out snapDeltaX, out snapDeltaY);
						// ToDo: Restore ResizeModifiers
						Assert(PreviewShape != null);
						if (PreviewShape != null)
							PreviewShape.MoveControlPointTo(ControlPointId.LastVertex, mouseState.X + snapDeltaX, mouseState.Y + snapDeltaY, ResizeModifiers.None);
					}
					Invalidate(ActionDiagramPresenter);
					break;

				case Action.DrawLine:
				case Action.MovePoint:
					throw new NotImplementedException();

				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}
			return result;
		}


		private bool ProcessMouseClick(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			Debug.Print("ProcessMouseClick");
			bool result = false;
			switch (CurrentAction) {
				case Action.None:
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						// If no other ToolAction is in Progress (e.g. drawing a line or moving a point),
						// a normal MouseClick starts a new line in Point-By-Point mode
						if (diagramPresenter.Project.SecurityManager.IsGranted(Permission.Insert)) {
							action = Action.AddPoint;
							StartLine(diagramPresenter, mouseState);
						}
					} else if (mouseState.IsButtonDown(MouseButtonsDg.Right)) {
						Cancel();
						result = true;
					}
					break;

				case Action.AddPoint:
					if (mouseState.IsButtonDown(MouseButtonsDg.Left)) {
						Invalidate(ActionDiagramPresenter);
						bool doFinishLine = false;
						// If the line has reached the MaxVertexCount limit, create it
						if (PreviewLinearShape.VertexCount >= PreviewLinearShape.MaxVertexCount)
							doFinishLine = true;
						else {
							InsertNewPoint(ActionDiagramPresenter, mouseState);
							// Check if it has to be connected to a shape or connection point
							if (CurrentAction == Action.AddPoint) {
								ShapeAtCursorInfo shapeAtCursorInfo = base.FindShapeAtCursor(ActionDiagramPresenter, mouseState.X, mouseState.Y, ControlPointCapabilities.Connect, diagramPresenter.ZoomedGripSize, false);
								if (!shapeAtCursorInfo.IsEmpty && !shapeAtCursorInfo.IsCursorAtGluePoint)
									doFinishLine = true;
							}
						}
						// Create line if necessary
						if (doFinishLine) {
							FinishLine(ActionDiagramPresenter, mouseState, false);
							while (IsToolActionPending)
								EndToolAction();
							OnToolExecuted(ExecutedEventArgs);
						}
						result = true;
					} else if (mouseState.IsButtonDown(MouseButtonsDg.Right)) {
						Assert(PreviewShape != null);
						if (PreviewLinearShape.VertexCount <= PreviewLinearShape.MinVertexCount)
							Cancel();
						else {
							FinishLine(ActionDiagramPresenter, mouseState, false);
							while (IsToolActionPending)
								EndToolAction();
							OnToolExecuted(ExecutedEventArgs);
						}
						result = true;
					}
					break;

				case Action.DrawLine:
				case Action.MovePoint:
					throw new NotImplementedException();
				default: throw new NShapeUnsupportedValueException(CurrentAction);
			}
			Invalidate(diagramPresenter);
			return result;
		}


		private bool ProcessDoubleClick(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			Debug.Print("ProcessDoubleClick");
			bool result = false;
			if (IsToolActionPending) {
				Assert(PreviewShape != null);
				FinishLine(ActionDiagramPresenter, mouseState, true);
				while (IsToolActionPending)
					EndToolAction();
				result = true;
			}
			OnToolExecuted(ExecutedEventArgs);
			return result;
		}


		private ILinearShape PreviewLinearShape {
			get { return (ILinearShape)PreviewShape; }
		}


		private Action CurrentAction {
			get { return action; }
		}


		/// <summary>
		/// Creates a new preview line shape
		/// </summary>
		private void StartLine(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			// Try to find a connection target
			ShapeAtCursorInfo targetShapeInfo = FindConnectionTarget(diagramPresenter, mouseState.X, mouseState.Y, false);

			int snapDeltaX = 0, snapDeltaY = 0;
			if (diagramPresenter.SnapToGrid) {
				if (targetShapeInfo.IsEmpty || targetShapeInfo.ControlPointId == ControlPointId.Reference)
					FindNearestSnapPoint(diagramPresenter, mouseState.X, mouseState.Y, out snapDeltaX, out snapDeltaY);
				else {
					Point p = targetShapeInfo.Shape.GetControlPointPosition(targetShapeInfo.ControlPointId);
					snapDeltaX = p.X - mouseState.X;
					snapDeltaY = p.Y - mouseState.Y;
				}
			}

			// set line's start coordinates
			Point start = Point.Empty;
			if (!targetShapeInfo.IsEmpty) {
				if (targetShapeInfo.ControlPointId == ControlPointId.Reference) {
					// ToDo: Get nearest point on line
					start = mouseState.Position;
					start.Offset(snapDeltaX, snapDeltaY);
				} else
					start = targetShapeInfo.Shape.GetControlPointPosition(targetShapeInfo.ControlPointId);
			} else {
				start = mouseState.Position;
				start.Offset(snapDeltaX, snapDeltaY);
			}
			// Start ToolAction
			StartToolAction(diagramPresenter, (int)Action.AddPoint, mouseState, true);

			// create new preview shape
			CreatePreview(diagramPresenter);
			// ToDo: Reactivate ResizeModifiers
			PreviewShape.MoveControlPointTo(ControlPointId.FirstVertex, start.X, start.Y, ResizeModifiers.None);
			PreviewShape.MoveControlPointTo(ControlPointId.LastVertex, mouseState.X, mouseState.Y, ResizeModifiers.None);
			// Connect to target shape if possible
			if (targetShapeInfo.IsCursorAtConnectionPoint) {
				if (CanConnectTo(PreviewShape, ControlPointId.FirstVertex, targetShapeInfo.Shape))
					PreviewShape.Connect(ControlPointId.FirstVertex, targetShapeInfo.Shape, targetShapeInfo.ControlPointId);
			}
			lastInsertedPointId = ControlPointId.FirstVertex;
		}


		/// <summary>
		/// Inserts a new point into the current preview line before the end point (that is sticking to the mouse cursor).
		/// </summary>
		private void InsertNewPoint(IDiagramPresenter diagramPresenter, MouseState mouseState) {
			Assert(PreviewLinearShape != null);
			if (PreviewLinearShape.VertexCount < PreviewLinearShape.MaxVertexCount) {
				ControlPointId existingPointId = ControlPointId.None;
				Point pointPos = PreviewShape.GetControlPointPosition(ControlPointId.LastVertex);
				foreach (ControlPointId ptId in PreviewShape.GetControlPointIds(ControlPointCapabilities.All)) {
					if (ptId == ControlPointId.Reference) continue;
					if (ptId == ControlPointId.LastVertex) continue;
					Point p = PreviewShape.GetControlPointPosition(ptId);
					if (p == pointPos && ptId != ControlPointId.Reference) {
						existingPointId = ptId;
						break;
					}
				}
				if (existingPointId == ControlPointId.None)
					lastInsertedPointId = PreviewLinearShape.InsertVertex(ControlPointId.LastVertex, pointPos.X, pointPos.Y);
			} else throw new InvalidOperationException(string.Format("Maximum number of verticex reached: {0}", PreviewLinearShape.MaxVertexCount));
		}


		/// <summary>
		/// Creates a new LinearShape and inserts it into the diagram of the CurrentDisplay by executing a Command.
		/// </summary>
		/// <param name="createWithAllPoints">If true, the line will be created as a 
		/// clone of the preview shape. If false, the line will be created until the 
		/// last point inserted. The point at the mouse cursor will be skipped.</param>
		private void FinishLine(IDiagramPresenter diagramPresenter, MouseState mouseState, bool ignorePointAtMouse) {
			Assert(PreviewShape != null);
			// Create a new shape from the template
			Shape newShape = Template.CreateShape();
			// Copy points from the PreviewShape to the new shape 
			// The current EndPoint of the preview (sticking to the mouse cursor) will be discarded
			foreach (ControlPointId pointId in PreviewShape.GetControlPointIds(ControlPointCapabilities.Resize)) {
				Point p = PreviewShape.GetControlPointPosition(pointId);
				// skip ReferencePoint and EndPoint
				switch (pointId) {
					case StartPointId:
					case EndPointId:
						// Check if there are any occurences left...
						Assert(false);
						break;
					case ControlPointId.Reference:
						continue;
					case ControlPointId.LastVertex:
						// * If the line *has no* vertex limit, the last point (sticking to the mouse cursor) will 
						//   always be discarded 
						// * If the line *has a* vertex limit, the last point will be created
						// * If the tool was cancelled, the last point will be discarded.
						// * If the line has not enough vertices to discard one, the last will be created at the 
						//	  position of the mouse
						if ((PreviewLinearShape.VertexCount == PreviewLinearShape.MaxVertexCount && !ignorePointAtMouse)
							|| PreviewLinearShape.VertexCount == PreviewLinearShape.MinVertexCount)
							newShape.MoveControlPointTo(ControlPointId.LastVertex, p.X, p.Y, ResizeModifiers.None);
						else continue;
						break;
					case ControlPointId.FirstVertex:
						newShape.MoveControlPointTo(ControlPointId.FirstVertex, p.X, p.Y, ResizeModifiers.None);
						break;
					default:
						// treat the last inserted Point as EndPoint
						if (ignorePointAtMouse && pointId == lastInsertedPointId)
							newShape.MoveControlPointTo(ControlPointId.LastVertex, p.X, p.Y, ResizeModifiers.None);
						else {
							if (pointId == lastInsertedPointId && PreviewLinearShape.VertexCount < PreviewLinearShape.MaxVertexCount)
								newShape.MoveControlPointTo(ControlPointId.LastVertex, p.X, p.Y, ResizeModifiers.None);
							else ((ILinearShape)newShape).InsertVertex(ControlPointId.LastVertex, p.X, p.Y);
						}
						break;
				}
			}

			// Create an aggregated command which performs creation of the new shape and 
			// connecting the new shapes to other shapes in one step
			AggregatedCommand aggregatedCommand = new AggregatedCommand();
			aggregatedCommand.Add(new InsertShapeCommand(ActionDiagramPresenter.Diagram, ActionDiagramPresenter.ActiveLayers, newShape, true, false));

			// Create connections
			foreach (ControlPointId gluePointId in newShape.GetControlPointIds(ControlPointCapabilities.Glue)) {
				ShapeConnectionInfo sci = PreviewShape.GetConnectionInfo(gluePointId, null);
				if (!sci.IsEmpty)
					aggregatedCommand.Add(new ConnectCommand(newShape, gluePointId, sci.OtherShape, sci.OtherPointId));
				else {
					// Create connection for the last vertex
					Point gluePtPos = PreviewShape.GetControlPointPosition(gluePointId);
					ShapeAtCursorInfo targetInfo = FindConnectionTarget(ActionDiagramPresenter, PreviewShape, ControlPointId.LastVertex, gluePtPos, false);
					if (!targetInfo.IsEmpty &&
						!targetInfo.IsCursorAtGluePoint &&
						targetInfo.ControlPointId != ControlPointId.None)
						aggregatedCommand.Add(new ConnectCommand(newShape, gluePointId, targetInfo.Shape, targetInfo.ControlPointId));
				}
			}

			// execute command and insert it into the history
			ActionDiagramPresenter.Project.ExecuteCommand(aggregatedCommand);
			// select the created ConnectorShape
			ActionDiagramPresenter.SelectShape(newShape, false);
		}


		/// <summary>
		/// Set the cursor for the current action
		/// </summary>
		private int DetermineCursor(IDiagramPresenter diagramPresenter, Shape shape, ControlPointId pointId) {
			switch (CurrentAction) {
				case Action.None:
				case Action.AddPoint:
					if (diagramPresenter.Project.SecurityManager.IsGranted(Permission.Layout)) {
						if (shape != null && shape is ILinearShape && pointId != ControlPointId.None)
							return cursors[ToolCursor.MovePoint];
						else if (pointId != ControlPointId.None) {
							if (Template.Shape.HasControlPointCapability(ControlPointId.LastVertex, ControlPointCapabilities.Glue)
								&& !shape.HasControlPointCapability(pointId, ControlPointCapabilities.Glue)
								&& shape.HasControlPointCapability(pointId, ControlPointCapabilities.Connect)) {
								return cursors[ToolCursor.Connect];
							}
						} else if (shape != null) {
							if (shape.HasControlPointCapability(ControlPointId.Reference, ControlPointCapabilities.Connect))
								return cursors[ToolCursor.Connect];
						}
						return cursors[ToolCursor.Pen];
					} else return cursors[ToolCursor.NotAllowed];

				case Action.DrawLine:
				case Action.MovePoint:
					throw new NotImplementedException();
				//if (display.Project.SecurityManager.IsGranted(Permission.Insert)) {
				//   if (shape != null && pointId > 0 &&
				//      !shape.HasControlPointCapability(pointId, ControlPointCapabilities.Glue) &&
				//      shape.HasControlPointCapability(pointId, ControlPointCapabilities.Connect)) {
				//      currentCursor = connectCursor;
				//   }
				//   else if (shape != null && shape.HasControlPointCapability(ControlPointId.Reference, ControlPointCapabilities.Connect)) 
				//      currentCursor = connectCursor;
				//   else currentCursor = penCursor;
				//}
				//else currentCursor = notAllowedCursor;
				//break;

				default: throw new NShapeUnsupportedValueException(action);
			}
		}


		private enum Action { None, DrawLine, AddPoint, MovePoint }

		private enum ToolCursor {
			Default,
			NotAllowed,
			MovePoint,
			Pen,
			Connect,
			Disconnect
		}


		#region Fields

		// Definition of the tool
		private static Dictionary<ToolCursor, int> cursors;
		//
		private const int StartPointId = 1;
		private const int EndPointId = 2;

		// Tool's state definition
		// stores the last inserted Point (and its coordinates), which will become the EndPoint when the CurrentTool is cancelled
		private ControlPointId lastInsertedPointId;
		private Action action;

		#endregion
	}


	/// <summary>
	/// Lets the user place a new shape on the diagram.
	/// </summary>
	public class PlanarShapeCreationTool : TemplateTool {

		public PlanarShapeCreationTool(Template template)
			: base(template) {
			Construct(template);
		}


		public PlanarShapeCreationTool(Template template, string category)
			: base(template, category) {
			Construct(template);
		}


		/// <override></override>
		public override bool ProcessMouseEvent(IDiagramPresenter diagramPresenter, MouseEventArgsDg e) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			bool result = false;

			// Return if action is not allowed
			if (!diagramPresenter.Project.SecurityManager.IsGranted(Permission.Insert))
				return result;

			MouseState newMouseState = MouseState.Empty;
			newMouseState.Buttons = e.Buttons;
			newMouseState.Modifiers = e.Modifiers;
			diagramPresenter.ControlToDiagram(e.Position, out newMouseState.Position);

			diagramPresenter.SuspendUpdate();
			try {
				switch (e.EventType) {
					case MouseEventType.MouseMove:
						if (newMouseState.Position != CurrentMouseState.Position) {
							// If no Preview exists, create a new one by starting a new ToolAction
							if (!IsToolActionPending)
								StartToolAction(diagramPresenter, (int)Action.Create, newMouseState, false);

							Invalidate(ActionDiagramPresenter);
							// Move preview shape to Mouse Position
							PreviewShape.MoveTo(newMouseState.X, newMouseState.Y);
							// Snap to grid
							if (diagramPresenter.SnapToGrid) {
								int snapDeltaX = 0, snapDeltaY = 0;
								FindNearestSnapPoint(diagramPresenter, PreviewShape, 0, 0, out snapDeltaX, out snapDeltaY);
								PreviewShape.MoveTo(newMouseState.X + snapDeltaX, newMouseState.Y + snapDeltaY);
							}
							Invalidate(ActionDiagramPresenter);
							result = true;
						}
						break;

					case MouseEventType.MouseUp:
						if (IsToolActionPending && newMouseState.IsButtonDown(MouseButtonsDg.Left)) {
							// Left mouse button was pressed: Create shape
							Invalidate(ActionDiagramPresenter);
							int x = PreviewShape.X;
							int y = PreviewShape.Y;

							ICommand cmd;
							Shape newShape = Template.CreateShape();
							newShape.ZOrder = ActionDiagramPresenter.Project.Repository.ObtainNewTopZOrder(ActionDiagramPresenter.Diagram);
							cmd = new InsertShapeCommand(ActionDiagramPresenter.Diagram, ActionDiagramPresenter.ActiveLayers, newShape, true, true, x, y);
							ActionDiagramPresenter.Project.ExecuteCommand(cmd);

							newShape = ActionDiagramPresenter.Diagram.Shapes.FindShape(x, y, ControlPointCapabilities.None, 0, null);
							if (newShape != null) diagramPresenter.SelectShape(newShape, false);
							EndToolAction();
							result = true;

							OnToolExecuted(ExecutedEventArgs);
						} else if (newMouseState.IsButtonDown(MouseButtonsDg.Right)) {
							// Right mouse button was pressed: Cancel Tool
							Cancel();
							result = true;
						}
						break;

					case MouseEventType.MouseDown:
						// nothing to to yet
						// ToDo 3: Implement dragging a frame with the mouse and fit the shape into that frame when releasing the button
						break;

					default: throw new NShapeUnsupportedValueException(e.EventType);
				}
			} finally { diagramPresenter.ResumeUpdate(); }
			base.ProcessMouseEvent(diagramPresenter, e);
			return result;
		}


		/// <override></override>
		public override bool ProcessKeyEvent(IDiagramPresenter diagramPresenter, KeyEventArgsDg e) {
			return base.ProcessKeyEvent(diagramPresenter, e);
		}


		/// <override></override>
		public override void EnterDisplay(IDiagramPresenter diagramPresenter) {
			if (!CurrentMouseState.IsEmpty)
				StartToolAction(diagramPresenter, (int)Action.Create, CurrentMouseState, false);
		}


		/// <override></override>
		public override void LeaveDisplay(IDiagramPresenter diagramPresenter) {
			EndToolAction();
		}


		/// <override></override>
		public override void Draw(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (drawPreview) {
				//if (DisplayContainsMousePos(ActionDisplay, CurrentMouseState.Position)) {
				diagramPresenter.DrawShape(PreviewShape);
				if (ActionDiagramPresenter.SnapToGrid)
					diagramPresenter.DrawSnapIndicators(PreviewShape);
				//}
			}
		}


		/// <override></override>
		public override void Invalidate(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			if (PreviewShape != null && diagramPresenter.SnapToGrid)
				diagramPresenter.InvalidateSnapIndicators(PreviewShape);
		}


		/// <override></override>
		public override IEnumerable<MenuItemDef> GetMenuItemDefs(IDiagramPresenter diagramPresenter) {
			if (diagramPresenter == null) throw new ArgumentNullException("diagramPresenter");
			yield break;
		}


		/// <override></override>
		protected override void CancelCore() {
			if (PreviewShape != null)
				ClearPreview();
		}


		/// <override></override>
		protected override void StartToolAction(IDiagramPresenter diagramPresenter, int action, MouseState mouseState, bool wantAutoScroll) {
			base.StartToolAction(diagramPresenter, action, mouseState, wantAutoScroll);
			CreatePreview(ActionDiagramPresenter);
			PreviewShape.DisplayService = diagramPresenter.DisplayService;
			PreviewShape.MoveTo(mouseState.X, mouseState.Y);
			drawPreview = true;
			diagramPresenter.SetCursor(CurrentCursorId);
		}


		/// <override></override>
		protected override void EndToolAction() {
			base.EndToolAction();
			drawPreview = false;
			ClearPreview();
		}


		static PlanarShapeCreationTool() {
			crossCursorId = CursorProvider.RegisterCursor(Properties.Resources.CrossCursor);
		}


		private void Construct(Template template) {
			if (!(template.Shape is IPlanarShape))
				throw new NShapeException("The template's shape does not implement {0}.", typeof(IPlanarShape).Name);
			drawPreview = false;
		}


		private int CurrentCursorId {
			get { return drawPreview ? crossCursorId : CursorProvider.DefaultCursorID; }
		}


		private enum Action { None, Create}


		#region Fields

		// Definition of the tool
		private static int crossCursorId;
		private bool drawPreview;

		#endregion
	}

}