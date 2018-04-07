namespace LilyViewer

type IAddedControls =

    abstract OnSize : float -> float -> bool -> unit

    abstract GetViewType : unit -> int