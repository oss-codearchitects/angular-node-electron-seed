import { Request, Response } from "express";

/**
 * GET /
 * Home page.
 */
export let index = (req: Request, res: Response) => {
    let title = "Hello buddy!";
    res.render('home', { title: title });
};

/**
 * GET /
 * Local page.
 */
export let local = (req: Request, res: Response) => {
  let title = "Hello buddy!";
  res.render('local', { title: title });
};
