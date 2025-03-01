import { useEffect, useState } from "react";
import Square from "./models/Square.ts";

function App() {
  const [squares, setSquares] = useState<Square[]>([]);

  useEffect(() => {
    GetSavedSquares();
  }, []);

  // Fetch saved squares from backend
  const GetSavedSquares = async () => {
    try {
      const response = await fetch("/api/GetSavedSquares");
      const data = await response.json();
      console.log("API Response:", data);

      const validSquares = data.map((s: Square) => ({
        id: s.id,
        color: s.color,
        x: s.x,
        y: s.y,
      }));

      setSquares(validSquares);
    } catch (error) {
      console.error("Error fetching saved squares:", error);
    }
  };

  const getSquare = async () => {
    try {
      const response = await fetch("/api/GetSquare");
  
      if (!response.ok) throw new Error("Failed to fetch square");
  
      const data = await response.json();
      console.log("API Response:", data);
  
      const newSquare = { ...data};
  
      setSquares((prevSquares) => [...prevSquares, newSquare]);
  
    } catch (error) {
      console.error("Error fetching square:", error);
    }
  };

  // Clear squares state and reset storage
  const clearState = async () => {
    try {
      await fetch("/api/ClearSquares", { method: "POST" });
      console.log("Cleared state");
      setSquares([]);
    } catch (error) {
      console.error("Error clearing state:", error);
    }
  };

  //Determine max positions dynamically
  const maxX = Math.max(...squares.map(s => s.x), 0);
  const maxY = Math.max(...squares.map(s => s.y), 0);

  const gridSize = { width: maxX + 1, height: maxY + 1 };

  return (
    <div className="flex flex-col items-center bg-gray-100 h-screen w-full">
      <h1 className="text-red-700 text-4xl mt-4">Squares</h1>

      <div className="flex gap-4 mt-4">
        <button onClick={getSquare} className="bg-blue-500 cursor-pointer text-white px-4 py-2 rounded-md hover:bg-blue-400">
          Square me
        </button>
        <button onClick={clearState} className="bg-blue-500 cursor-pointer text-white px-4 py-2 rounded-md hover:bg-blue-400">
          Clear State
        </button>
      </div>

      <div
        className="mt-4 grid gap-1 bg-gray-100"
        style={{
          display: "grid",
          gridTemplateColumns: `repeat(${gridSize.width}, 64px)`,
          gridTemplateRows: `repeat(${gridSize.height}, 64px)`,
        }}
      >
        {squares.map((square) => (
          <div
            key={square.id} 
            style={{
              gridColumn: square.x + 1, 
              gridRow: square.y + 1, 
            }}
            className={`border border-gray-300 ${square.color}`}
          ></div>
        ))}
      </div>
    </div>
  );
}

export default App;
