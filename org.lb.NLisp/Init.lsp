; TODO: let, and, or, equalp, assoc, string manipulation functions, cond macro

(defun list (&rest args) args)
(defun not (x) (if x nil t))
(defun <= (a b) (not (> a b)))
(defun >= (a b) (not (< a b)))
(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caddr (x) (car (cdr (cdr x))))
(defun abs (x) (if (< x 0) (- 0 x) x))
(defun evenp (x) (= 0 (mod x 2)))
(defun oddp (x) (not (evenp x)))

(defmacro incf (varname)
  (list 'setq varname (list '+ varname 1)))

(defmacro decf (varname)
  (list 'setq varname (list '- varname 1)))

(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (list
    (list 'lambda '(tos)
      (list 'setq listvar (list 'cdr listvar))
      'tos)
    (list 'car listvar)))

(defun map (f lst)
  (define ret nil)
  (while lst
    (push (f (car lst)) ret)
    (setq lst (cdr lst)))
  (nreverse ret))

(defun filter (f lst)
  (define ret nil)
  (while lst
    (if (f (car lst))
        (push (car lst) ret)
        nil)
    (setq lst (cdr lst)))
  (nreverse ret))

(defun reduce (f lst)
  (define acc (car lst))
  (setq lst (cdr lst))
  (if lst
      (while lst
        (setq acc (f acc (car lst)))
        (setq lst (cdr lst)))
      (setq acc nil))
  acc)

(defun every (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc (f item) nil))
        (cons t lst))
      t))

(defun some (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc acc (f item)))
        (cons nil lst))
      nil))

(defun range (&rest args)
  (define arglen (length args))
  (define from 0)
  (define to 0)
  (define step 1)
  (if (= 1 arglen)
      (setq to (car args))
      (progn
        (setq from (car args))
        (setq to (cadr args))
        (if (> arglen 2)
            (setq step (caddr args))
            nil)))
  (define ret nil)
  (while (< from to)
    (push from ret)
    (setq from (+ from step)))
  (reverse ret))
