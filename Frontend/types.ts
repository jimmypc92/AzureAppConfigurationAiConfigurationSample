export interface ChatMessage {
  role: string;
  content: string;
  timestamp: Date;
}

export interface ChatRequest {
  message: string;
  history?: ChatMessage[];
}

export interface ChatResponse {
  message: string;
  history: ChatMessage[];
}
