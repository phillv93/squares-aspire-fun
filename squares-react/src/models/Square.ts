//generate a square class with a color property
export default class Square {
  id: number;
  color: string;
  x: number;
  y: number;

  constructor(id: number, color: string, x: number, y: number) {
    this.id = id;
    this.color = color;
    this.x = x;
    this.y = y;
  }
}