export interface Book {
  isbn: string;
  title: string;
  language: string;
  authors: string[];
  year: number;
  price: number;
  category: string;
  cover?: string | null;
  authorsDisplay?: string;
}
